using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSharp.Interactive.Intellisense
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("any")]
    [Name("F# completion handler")]
    internal class FSharpCompletionSourceProvider : ICompletionSourceProvider
    {
        public FSharpCompletionSourceProvider() 
        {
        }

        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal IGlyphService GlyphService { get; set; }

        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new FSharpCompletionSource(this, textBuffer);
        }

    }
}
