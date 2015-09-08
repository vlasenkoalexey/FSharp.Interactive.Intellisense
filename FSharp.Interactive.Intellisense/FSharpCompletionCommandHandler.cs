using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.Windows.Input;
using System.ComponentModel.Design;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Reflection;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics;
using Task = System.Threading.Tasks.Task;
using FSharp.Interactive.Intellisense.Lib;

namespace FSharp.Interactive.Intellisense
{
    public class FSharpCompletionCommandHandler : IOleCommandTarget
    {
        private IOleCommandTarget m_nextCommandHandler;
        private ITextView m_textView;
        private FSharpCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;
        private IVsTextView textViewAdapter;
        private IOleCommandTarget fsiToolWindow;
        private EnvDTE.DTE dte;

        private AutocompleteModeType autocompleteMode
        {
            get
            {
                var settings = dte.get_Properties(FSharp_Interactive_IntellisensePackage.SettingsCategoryName, FSharp_Interactive_IntellisensePackage.SettingsPageName);
                AutocompleteModeType autocompleteMode = (AutocompleteModeType)settings.Item("AutocompleteMode").Value;
                return autocompleteMode;
            }
        }

        internal FSharpCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, FSharpCompletionHandlerProvider fsharpCompletionHandlerProvider)
        {
            this.m_textView = textView;
            this.m_provider = fsharpCompletionHandlerProvider;

            this.dte = this.m_provider.ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);

            Task.Delay(2000).ContinueWith((a) =>
            {
                // Probably it is possible to find it through package as well.
                this.fsiToolWindow = CommandChainNodeWrapper.GetFilterByFullClassName(textViewAdapter, 
                    FsiLanguageServiceHelper.FsiToolWindowClassName);
            });

            this.textViewAdapter = textViewAdapter;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (autocompleteMode == AutocompleteModeType.Off 
                || VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider))
            {
                return m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            //make a copy of this so we can look at it after forwarding some commands 
            uint commandID = nCmdID;
            char typedChar = char.MinValue;
            //make sure the input is a char before getting it 
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            //check for a commit character 
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
                || char.IsPunctuation(typedChar) && typedChar != '.')
            {

                //check for a a selection 
                if (m_session != null && !m_session.IsDismissed)
                {
                    //if the selection is fully selected, commit the current session 
                    if (m_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        m_session.Commit();
                        //also, don't add the character to the buffer 
                        if (typedChar != '.')
                        {
                            return VSConstants.S_OK;
                        }
                    }
                    else
                    {
                        //if there is no selection, dismiss the session
                        m_session.Dismiss();
                    }
                }
            }

            // Dismiss session when whitespace is pressed.
            if (char.IsWhiteSpace(typedChar) && m_session != null && !m_session.IsDismissed)
            {
                m_session.Dismiss();
            }

            //pass along the command so the char is added to the buffer 
            int retVal = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            bool handled = false;
            if (!typedChar.Equals(char.MinValue) && (char.IsLetterOrDigit(typedChar) || typedChar == '.') &&
                autocompleteMode == AutocompleteModeType.Automatic)
            {
                if (m_session == null || m_session.IsDismissed) // If there is no active session, bring up completion
                {
                    this.TriggerCompletion();
                    if (m_session != null && typedChar != '.')
                    {
                        m_session.Filter();
                    }
                }
                else     //the completion session is already active, so just filter
                {
                    if (m_session.SelectedCompletionSet != null &&
                        m_session.SelectedCompletionSet.Completions != null &&
                        m_session.SelectedCompletionSet.Completions.Count > 0 &&
                        m_session.SelectedCompletionSet.Completions[0].InsertionText.StartsWith("."))
                    {
                        // For sorm reason m_session.Filter() filters by DisplayName, not InsertionText no matter what.
                        // Probbly need to remimplement it in order to work properly, otherwise just restarting completion session.
                        m_session.Dismiss();
                        this.TriggerCompletion();
                    }
                    else
                    {
                        m_session.Filter();
                    }
                }

                if (m_session != null && m_session.Presenter != null)
                {
                    handled = true;
                    dynamic presenter = ExposedObject.From(m_session.Presenter);
                    IIntellisensePresenter intelisensePresenter = m_session.Presenter;
                    System.Windows.Controls.ContentControl surfaceElement = (System.Windows.Controls.ContentControl)presenter.SurfaceElement;
                    surfaceElement.Focus();
                }
            }
            else if ((autocompleteMode == AutocompleteModeType.Automatic || autocompleteMode == AutocompleteModeType.CtrlSpace) 
                && (commandID == (uint)VSConstants.VSStd2KCmdID.AUTOCOMPLETE || commandID == (uint)VSConstants.VSStd2KCmdID.COMPLETEWORD))
            {
                // Trigger completion on Ctrl + Space
                if (m_session == null || m_session.IsDismissed) // If there is no active session, bring up completion
                {
                    this.TriggerCompletion();
                    if (m_session != null && typedChar != '.')
                    {
                        m_session.Filter();
                    }
                }

            }
            else if (commandID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE   //redo the filter if there is a deletion
                || commandID == (uint)VSConstants.VSStd2KCmdID.DELETE)
            {
                if (m_session != null && !m_session.IsDismissed)
                    m_session.Filter();
                handled = true;
            }
            if (handled) return VSConstants.S_OK;
            return retVal;
        }

        private bool TriggerCompletion()
        {
            //the caret must be in a non-projection location 
            SnapshotPoint? caretPoint =
            m_textView.Caret.Position.Point.GetPoint(
            textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
            if (!caretPoint.HasValue)
            {
                return false;
            }

            m_session = m_provider.CompletionBroker.TriggerCompletion(m_textView);

            if (m_session == null)
            {
                return false;
            }

            //subscribe to the Dismissed event on the session 
            m_session.Dismissed += this.OnSessionDismissed;

            // TODO: wrap fsiToolWindow into class
            if (m_session != null)
            {
                SetFsiToolWindowCompletionSetVisibilty(true);
            }

            return true;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            m_session.Dismissed -= this.OnSessionDismissed;
            m_session = null;
            SetFsiToolWindowCompletionSetVisibilty(false);
        }

        private void SetFsiToolWindowCompletionSetVisibilty(bool displayed)
        {
            if (fsiToolWindow != null)
            {
                dynamic source = ExposedObject.From(fsiToolWindow).source;
                if (source != null)
                {
                    dynamic completionSet = ExposedObject.From(source).CompletionSet;
                    if (completionSet != null)
                    {
                        dynamic completionSetExp = ExposedObject.From(completionSet);
                        completionSetExp.displayed = displayed;
                    }
                }
            }
        }
    }
}
