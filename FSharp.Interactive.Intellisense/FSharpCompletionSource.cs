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

            var providerGlobal = (IOleServiceProvider)Package.GetGlobalService(typeof(IOleServiceProvider));
            var provider = new ServiceProvider(providerGlobal);
            dynamic fsiLanguageService = ExposedObject.From(provider.GetService(fsiLanguageServiceType));
            dynamic sessions = fsiLanguageService.sessions;
            dynamic sessionsValue = ExposedObject.From(sessions).Value;
            dynamic sessionR = ExposedObject.From(sessionsValue).sessionR;
            dynamic sessionRValue = ExposedObject.From(sessionR).Value;
            dynamic sessionRValueValue = ExposedObject.From(sessionRValue).Value;
            dynamic fsharpInteractiveService = ExposedObject.From(sessionRValueValue).client;
            //fsharpInteractiveService.Interrupt();

            //((Microsoft.VisualStudio.FSharp.Interactive.Session.createSessions@391-5)(sessions.Value)).sessionR.Value.Value

            // TODO: figure out how to execute some script in F# interactive programatically
            // TODO: figure out how FSI interactive service (server) is instantiated
            // TODO: figure out how FSI interactive service (client) is used
            // TODO: download VS FSI sources

            
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            List<string> strList = new List<string>();
            strList.Add("addition");
            strList.Add("adaptation");
            strList.Add("subtraction");
            strList.Add("summation");
            m_compList = new List<Completion>();
            foreach (string str in strList)
                m_compList.Add(new Completion(str, str, str, null, null));

            completionSets.Add(new CompletionSet(
                "Tokens",    //the non-localized title of the tab 
                "Tokens",    //the display title of the tab
                FindTokenSpanAtPosition(session.GetTriggerPoint(m_textBuffer),
                    session),
                m_compList,
                null));
        }

        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
        {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = m_sourceProvider.NavigatorService.GetTextStructureNavigator(m_textBuffer);
            TextExtent extent = navigator.GetExtentOfWord(currentPoint);
            return currentPoint.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);
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
