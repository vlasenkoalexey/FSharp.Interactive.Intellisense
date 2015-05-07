using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Reflection;
using FSharp.Interactive.Intellisense.Lib;
using System.Diagnostics;


namespace FSharp.Interactive.Intellisense
{
    internal class FSharpCompletionSource : ICompletionSource
    {
        private FSharpCompletionSourceProvider m_sourceProvider;
        private ITextBuffer m_textBuffer;
        private List<Completion> m_compList;


        public FSharpCompletionSource(FSharpCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            m_sourceProvider = sourceProvider;
            m_textBuffer = textBuffer;

            Assembly fsiAssembly = Assembly.Load("FSharp.VS.FSI, Version=12.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            Type fsiLanguageServiceType = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.FsiLanguageService");

            Type sessionsType = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.Session.Sessions");

            var providerGlobal = (IOleServiceProvider)Package.GetGlobalService(typeof(IOleServiceProvider));
            var provider = new ServiceProvider(providerGlobal);
            dynamic fsiLanguageService = ExposedObject.From(provider.GetService(fsiLanguageServiceType));
            dynamic sessions = fsiLanguageService.sessions;
            dynamic sessionsValue = ExposedObject.From(sessions).Value;
            dynamic sessionR = ExposedObject.From(sessionsValue).sessionR;
            dynamic sessionRValue = ExposedObject.From(sessionR).Value;
            dynamic sessionRValueValue = ExposedObject.From(sessionRValue).Value;
            MethodInfo methodInfo = fsiAssembly.GetType("Microsoft.VisualStudio.FSharp.Interactive.Session+Session").GetMethods()[1];
            dynamic fsiProcess = methodInfo.Invoke((Object)sessionRValueValue, null);

            fsiProcess.Invoke(String.Format("#r \"{0}\";;", typeof(AutocompleteServer).Assembly.Location));
            fsiProcess.Invoke("FSharp.Interactive.Intellisense.Lib.AutocompleteServer.StartServer(\"channel\");;");

            var fsiTypes = fsiProcess.GetType().Assembly.GetTypes();
            Assembly fsAssembly = fsiProcess.GetType().Assembly;


            //dynamic r = sessionRValueValue.ProcessID;
            //dynamic fsharpInteractiveService = ExposedObject.From(sessionRValueValue).client;
            //fsharpInteractiveService.Interrupt();

            //((Microsoft.VisualStudio.FSharp.Interactive.Session.createSessions@391-5)(sessions.Value)).sessionR.Value.Value

            // TODO: figure out how to execute some script in F# interactive programatically
            // TODO: figure out how FSI interactive service (server) is instantiated
            // TODO: figure out how FSI interactive service (client) is used
            // TODO: download VS FSI sources

            
        }

        static bool IsWhiteSpaceOrDelimiter(char p)
        {
            switch (p)
            {
                case '(':
                case '[':
                case ' ':
                    return true;
            }
            return false;
        }

        private AutocompleteService autocomplteService;

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            //var triggerPoint = session.GetTriggerPoint(m_textBuffer.CurrentSnapshot);
            //if (triggerPoint == null)
            //    return;

            //var applicableTo1 = m_textBuffer.CurrentSnapshot.CreateTrackingSpan(new SnapshotSpan(triggerPoint.Value, 1), SpanTrackingMode.EdgeInclusive);


            ITextSnapshot snapshot = m_textBuffer.CurrentSnapshot;
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(snapshot);
            if (triggerPoint == null)
            {
                return;
            }
            SnapshotPoint end = triggerPoint.Value;
            SnapshotPoint start = end;
            // go back to either a delimiter, a whitespace char or start of line.
            while (start > 0)
            {
                SnapshotPoint prev = start - 1;
                if (IsWhiteSpaceOrDelimiter(prev.GetChar()))
                {
                    break;
                }
                start += -1;
            }

            var span = new SnapshotSpan(start, end);
            // The ApplicableTo span is what text will be replaced by the completion item
            ITrackingSpan applicableTo1 = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);

            String statement = applicableTo1.GetText(applicableTo1.TextBuffer.CurrentSnapshot);

            if (autocomplteService == null)
            {
                try
                {
                    autocomplteService = AutocompleteServer.StartClient("channel");

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            IEnumerable<String> completions = new List<String>();
            if (autocomplteService != null)
            {
                try
                {
                    int result = autocomplteService.Test();
                    completions = autocomplteService.GetCompletions(statement);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }            
            
            //IEnumerable<String> completions = GetSuggestionsForType(statement);
            //List<string> strList = new List<string>();
            //strList.Add("addition");
            //strList.Add("adaptation");
            //strList.Add("subtraction");
            //strList.Add("summation");
            m_compList = new List<Completion>();
            foreach (string str in completions)
                m_compList.Add(new Completion(str, str, str, null, null));

            var applicableTo = FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer),
                    session);

            completionSets.Add(new CompletionSet(
                "Tokens",    //the non-localized title of the tab 
                "Tokens",    //the display title of the tab
                applicableTo,
                m_compList,
                null));
        }

        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
        {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            //TextExtent extent = navigator.get(currentPoint);
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
        }

        private static Type[] GetTypes()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(object));
            Type[] types = assembly.GetTypes();
            return types;
        }

        private static bool TypeEqualsStatement(string statementTrimmed, Type x)
        {
            // if we have an empty statement then it's impossible to have a type
            if (string.IsNullOrWhiteSpace(statementTrimmed))
                return false;

            // Get the last chain
            var lastSeperator = statementTrimmed.LastIndexOf('.');
            lastSeperator = lastSeperator == -1 ? 0 : lastSeperator;
            var statementedSubtrimmed = statementTrimmed.Substring(0, lastSeperator);

            // Compare to the statement minus the last chain and the last chain because the last chain can be a part of the type member
            return x.FullName.Equals(statementTrimmed) || (!string.IsNullOrEmpty(statementedSubtrimmed) && x.FullName.Equals(statementedSubtrimmed));
        }

        private static IEnumerable<string> GetSuggestionsForType(string statement)
        {
            if (statement.LastIndexOf('.') < 0)
            {
                return new List<String>();
            }
            var types = GetTypes();
            var type = types.FirstOrDefault(x => TypeEqualsStatement(statement, x));
            var lastChain = statement.Substring(statement.LastIndexOf('.')).Trim('.', ' ');

            if (type == null)
                return new List<string>();

            // if the last chain is the type itself then we give a full list of it's members
            if (type.FullName.EndsWith(lastChain))
                return type.GetMembers().Select(x => x.Name)
                                        .Distinct();

            // if the last chain isn't the type itself then we filter the members
            return type.GetMembers().Where(x => x.Name.Contains(lastChain))
                                    .Select(x => x.Name)
                                    .Distinct();
        }

        private bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }
}
