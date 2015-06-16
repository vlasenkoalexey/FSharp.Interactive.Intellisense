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
using System.Reflection;
using FSharp.Interactive.Intellisense.Lib;
using System.Diagnostics;
using StatementCompletion = FSharp.Interactive.Intellisense.Lib.Completion;
using Completion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace FSharp.Interactive.Intellisense
{
    internal class FSharpCompletionSource : ICompletionSource
    {
        private FSharpCompletionSourceProvider sourceProvider;
        private ITextBuffer textBuffer;
        private List<Completion> compList;

        public FSharpCompletionSource(FSharpCompletionSourceProvider sourceProvider, ITextBuffer textBuffer)
        {
            this.sourceProvider = sourceProvider;
            this.textBuffer = textBuffer;
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
            ITextSnapshot snapshot = textBuffer.CurrentSnapshot;
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
                    autocomplteService = AutocompleteClient.GetAutocompleteService();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }

            IEnumerable<Tuple<String, int>> completions = new List<Tuple<String, int>>();
            if (autocomplteService != null)
            {
                try
                {
                    completions = autocomplteService.GetCompletions(statement);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }            
            
            compList = new List<Completion>();
            bool prependDot = statement.EndsWith(".");

            foreach (Tuple<String, int> tuple in completions)
            {
                StatementCompletion completion = StatementCompletion.FromTuple(tuple.Item1, tuple.Item2);
                string str = completion.Text;
                var glyph = sourceProvider.GlyphService.GetGlyph(CompletionTypeToStandardGlyphGroup(completion.CompletionType),
                    StandardGlyphItem.GlyphItemPublic);
                compList.Add(new Completion(str, prependDot ? "." + str : str, str, glyph, null));
            }

            var applicableTo = FindTokenSpanAtPosition(session.GetTriggerPoint(textBuffer),
                    session);

            CompletionSet completionSet = new CompletionSet(
                "F# completions",    //the non-localized title of the tab 
                "F# completions",    //the display title of the tab
                applicableTo,
                compList,
                null);

            // Following code doesn't work:
            //ExposedObject.From(completionSet)._filterMatchType =
            //    Microsoft.VisualStudio.Language.Intellisense.CompletionMatchType.MatchInsertionText;

            completionSets.Add(completionSet);
        }

        private static StandardGlyphGroup CompletionTypeToStandardGlyphGroup(CompletionType completionType)
        {
            switch(completionType)
            {
                case CompletionType.Namespace:
                    return StandardGlyphGroup.GlyphGroupNamespace;
                case CompletionType.Class:
                    return StandardGlyphGroup.GlyphGroupClass;
                case CompletionType.Module:
                    return StandardGlyphGroup.GlyphGroupModule;
                case CompletionType.Method:
                    return StandardGlyphGroup.GlyphGroupMethod;
                case CompletionType.Variable:
                    return StandardGlyphGroup.GlyphGroupVariable;
                default:
                    return StandardGlyphGroup.GlyphGroupUnknown;
            }
        }

        private ITrackingSpan FindTokenSpanAtPosition(ITrackingPoint point, ICompletionSession session)
        {
            SnapshotPoint currentPoint = (session.TextView.Caret.Position.BufferPosition) - 1;
            ITextStructureNavigator navigator = sourceProvider.NavigatorService.GetTextStructureNavigator(textBuffer);
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
