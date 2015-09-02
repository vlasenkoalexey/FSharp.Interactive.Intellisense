using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSharp.Interactive.Intellisense
{
    public class OptionsPageGrid : DialogPage
    {
        public enum IntellisenseProviderEnum
        {
            Combined,
            FsiSession,
            Internal
        }

        [Category("Settings")]
        [DisplayName("Autocomplete mode")]
        [Description("The way autocomplete is triggered (automatically, when Ctrl + Space is pressed, or Off).")]
        public AutocompleteModeEnum AutocompleteMode
        {
            get;
            set;
        }

        [Category("Settings")]
        [DisplayName("Intellisense provider")]
        [Description("Type of completions provider for Intellisense.")]
        public IntellisenseProviderEnum IntellisenseProvider
        {
            get;
            set;
        }
    }
}
