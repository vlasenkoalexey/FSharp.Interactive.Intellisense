using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSharp.Interactive.Intellisense
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("F# completion")]
    [ContentType("any")] // F# and FSharpInteractive do not work here.
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class FSharpCompletionHandlerProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService = null;
        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }
        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }
        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
            {
                return;
            }

            var fsiViewFilter = CommandChainNodeWrapper.GetFilterByFullClassName(textViewAdapter, FsiLanguageServiceHelper.FsiViewFilterClassName);
            if (fsiViewFilter == null)
            {
                return; // I didn't find a btter way to figure out if this IVsTextView is indeed F# interactive.
            }

            FsiLanguageServiceHelper fsiLanguageServiceHelper = new FsiLanguageServiceHelper();
            fsiLanguageServiceHelper.StartRegisterAutocompleteWatchdogLoop();

            Func<FSharpCompletionCommandHandler> createCommandHandler = delegate() 
            { 
                return new FSharpCompletionCommandHandler(textViewAdapter, textView, this); 
            };
            textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
        }

        public Package GetPackage()
        {
            try
            {
                return ExposedObject.From(ServiceProvider).ServiceProvider as Package;
            }
            catch
            {
                return null;
            }
        }

    }
}
