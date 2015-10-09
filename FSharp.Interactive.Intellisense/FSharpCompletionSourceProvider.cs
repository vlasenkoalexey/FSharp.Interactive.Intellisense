using Microsoft.VisualStudio.Editor;
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
    [ContentType("text")]
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

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import]
        internal FSharpCompletionHandlerProvider FSharpCompletionHandlerProvider { get; set; }


        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            // There is no way to filter F# intellisense provider by content type since it is "text", 
            // therefore have to do this small hack to trigger completions only for F# intellisense TextView.
            if (FSharpCompletionHandlerProvider != null && 
                FSharpCompletionHandlerProvider.TextView != null && 
                FSharpCompletionHandlerProvider.TextView.TextBuffer == textBuffer)
            {
                return new FSharpCompletionSource(this, textBuffer);
            }
            else
            {
                return null;
            }
        }

    }
}
