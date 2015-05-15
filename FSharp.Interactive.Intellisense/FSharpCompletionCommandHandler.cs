﻿using System;
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

namespace FSharp.Interactive.Intellisense
{
    public class FSharpCompletionCommandHandler : IOleCommandTarget
    {
        private IOleCommandTarget m_nextCommandHandler;
        private ITextView m_textView;
        private FSharpCompletionHandlerProvider m_provider;
        private ICompletionSession m_session;
        private IVsTextView textViewAdapter;

        internal FSharpCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, FSharpCompletionHandlerProvider fsharpCompletionHandlerProvider)
        {

            this.m_textView = textView;
            this.m_provider = fsharpCompletionHandlerProvider;

            //IOleCommandTarget fsiViewFilter =
            //    CommandChainNodeWrapper.GetFilterByFullClassName(new CommandChainNodeWrapper(textViewAdapter),
            //        "Microsoft.VisualStudio.FSharp.Interactive.FsiViewFilter");


            //if (fsiViewFilter != null)
            //{
            //    textViewAdapter.RemoveCommandFilter(fsiViewFilter);
            //}

            //IOleCommandTarget intellisenseFilter =
            //    CommandChainNodeWrapper.GetFilterByFullClassName(new CommandChainNodeWrapper(textViewAdapter),
            //        "Microsoft.VisualStudio.Editor.Implementation.IntellisenseCommandFilter");

            //if (intellisenseFilter != null)
            //{
            //    textViewAdapter.RemoveCommandFilter(intellisenseFilter);
            //}

            //add the command to the command chain

            Task.Delay(1000).ContinueWith((a) =>
                {
                    textViewAdapter.AddCommandFilter(this, out m_nextCommandHandler);
                });

            //textViewAdapter.RemoveCommandFilter()

            this.textViewAdapter = textViewAdapter;
            //((System.Windows.Controls.Control)this.m_textView).AddHandler(.KeyDown += (h, e) =>
            //    {
            //        if (e.Key == System.Windows.Input.Key.Down)
            //        {
            //            e.Handled = true;
            //        }
            //    };


            //((System.Windows.Controls.ContentControl)(((System.Windows.Controls.Grid)(((System.Windows.Controls.Control)(sender)).Parent)).Parent))
            ((System.Windows.Controls.Control)this.m_textView).AddHandler(System.Windows.Window.PreviewKeyDownEvent, 
                new KeyEventHandler(ControlViewer_KeyUp), true);

            // TODO: figure out what exception is thrown ++++
            //RegisterKeyDown(textView);
            // TODO: read about adding up / down events to wpf textbox
            // TODO: try attaching to the parent element (see above)
            // TODO: look in F# intellisense code, how pagedown is handled +++
            // TODO: test that select next/previous apprach works
            // TODO: copy custom visual provider from https://github.com/techtalk/SpecFlow.VisualStudio/blob/master/VsIntegration/AutoComplete/IntellisensePresenter/CompletionSessionPresenter.cs
        
            // TODO: use reflection to find FsiFilter in command chain 
            //((Microsoft.VisualStudio.Editor.Implementation.CommandChainNode)(((Microsoft.VisualStudio.Editor.Implementation.CommandChainNode)(((Microsoft.VisualStudio.Editor.Implementation.CommandChainNode)(((Microsoft.VisualStudio.Editor.Implementation.SimpleTextViewWindow)(textViewAdapter))._commandChain.Next)).Next)).Next))
            // Remove it and add only after my filer

        }

        private class CommandChainNodeWrapper
        {
            public IOleCommandTarget FilterObject
            {
                get
                {
                    return commandChainNode.FilterObject;
                }
            }

            public CommandChainNodeWrapper Next
            {
                get
                {
                    if (commandChainNode.Next != null)
                    {
                        return new CommandChainNodeWrapper(commandChainNode.Next);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            private dynamic commandChainNode;

            public CommandChainNodeWrapper(Object commandChainNode)
            {
                this.commandChainNode = ExposedObject.From(commandChainNode);
            }

            public CommandChainNodeWrapper(IVsTextView textAdapter)
            {
                FieldInfo commandChainFieldInfo = textAdapter.GetType().BaseType.GetField("_commandChain", BindingFlags.NonPublic | BindingFlags.Instance);
                this.commandChainNode = new CommandChainNodeWrapper(commandChainFieldInfo.GetValue(textAdapter));
            }

            public static IOleCommandTarget GetFilterByFullClassName(CommandChainNodeWrapper commandChainWrapper, string className)
            {
                if (commandChainWrapper.FilterObject != null && commandChainWrapper.FilterObject.GetType().FullName == className)
                {
                    return commandChainWrapper.FilterObject as IOleCommandTarget;
                }

                if (commandChainWrapper.Next != null)
                {
                    return GetFilterByFullClassName(commandChainWrapper.Next, className);
                }

                return null;
            }
        }



        private void MenuCommandCallback(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show(sender.ToString());
        }

        private void RegisterKeyDown(ITextView textView)
        {
            OleMenuCommandService oleMenuCommandService;
            CommandID commandID;
            MenuCommand menuCommand;
            
            //var providerGlobal = (IOleServiceProvider)Package.GetGlobalService(typeof(IOleServiceProvider));
            
            //var provider = new ServiceProvider(providerGlobal);

            Package package = m_provider.GetPackage();
            //OleMenuCommandService oleMenuCommandService = ExposedObject.From(package).GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            //Assembly fsiAssembly = Assembly.Load("FSharp.VS.FSI, Version=12.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            //Type fsiToolWindowType = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.FsiToolWindow");

            //IVsOutputWindowPane fsiToolWindowPane = package.GetOutputPane(new Guid("dee22b65-9761-4a26-8fb2-759b971d6dfc"), "F# Interactive");

           

            //oleMenuCommandService = provider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            oleMenuCommandService = new OleMenuCommandService(package);

            if (oleMenuCommandService != null)
            {
                var guidVSStd2KCmdID = typeof(VSConstants.VSStd2KCmdID).GUID;
                commandID = new CommandID(guidVSStd2KCmdID, (int)VSConstants.VSStd2KCmdID.RIGHT);

                menuCommand = new MenuCommand(MenuCommandCallback, commandID);

                oleMenuCommandService.AddCommand(menuCommand);
            }
        }

        private void ControlViewer_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Tab || e.Key == Key.Space || e.Key == Key.Up)
            {
                e.Handled = true;
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            //if (pguidCmdGroup == VSConstants.VSStd2K)
            //{
            //    Debug.WriteLine(String.Format("QueryStatus up[{0}] down[{1}] value[{2}]", (uint)VSConstants.VSStd2KCmdID.UP,
            //        (uint)VSConstants.VSStd2KCmdID.DOWN, ((prgCmds != null && prgCmds.Length > 0) ? prgCmds[0].cmdID.ToString() : "")));
            //}
            return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            //IOleCommandTarget fsiToolWindowFilter =
            //    CommandChainNodeWrapper.GetFilterByFullClassName(new CommandChainNodeWrapper(textViewAdapter),
            //        "Microsoft.VisualStudio.FSharp.Interactive.FsiToolWindow");


            Debug.WriteLine(String.Format("Exec up[{0}] down[{1}] value[{2}]", (uint)VSConstants.VSStd2KCmdID.UP,
                (uint)VSConstants.VSStd2KCmdID.DOWN, nCmdID.ToString()));

            if (VsShellUtilities.IsInAutomationFunction(m_provider.ServiceProvider))
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

            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.DOWN || nCmdID == (uint)VSConstants.VSStd2KCmdID.UP)
            {
                return VSConstants.S_OK;
            }

            //check for a commit character 
            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
                || (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar)))
            {

                //check for a a selection 
                if (m_session != null && !m_session.IsDismissed)
                {
                    //if the selection is fully selected, commit the current session 
                    if (m_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        m_session.Commit();
                        //also, don't add the character to the buffer 
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        //if there is no selection, dismiss the session
                        m_session.Dismiss();
                    }
                }
            }

            //pass along the command so the char is added to the buffer 
            int retVal = m_nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            bool handled = false;
            if (!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar))
            {
                if (m_session == null || m_session.IsDismissed) // If there is no active session, bring up completion
                {
                    this.TriggerCompletion();
                    if (m_session != null)
                    {
                        m_session.Filter();
                    }
                }
                else     //the completion session is already active, so just filter
                {
                    m_session.Filter();
                }

                dynamic presenter = ExposedObject.From(m_session.Presenter);
                IIntellisensePresenter intelisensePresenter = m_session.Presenter;
                System.Windows.Controls.ContentControl surfaceElement = (System.Windows.Controls.ContentControl)presenter.SurfaceElement;
                surfaceElement.KeyDown += (h, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Down)
                    {
                        e.Handled = true;
                    }
                };
                surfaceElement.KeyUp += (h, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Down)
                    {
                        e.Handled = true;
                    }
                };
                surfaceElement.Focus();

                handled = true;
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

            m_session = m_provider.CompletionBroker.CreateCompletionSession
         (m_textView,
                caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
                true);

            //subscribe to the Dismissed event on the session 
            m_session.Dismissed += this.OnSessionDismissed;
            m_session.Start();

            return true;
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            m_session.Dismissed -= this.OnSessionDismissed;
            m_session = null;
        }
    }
}
