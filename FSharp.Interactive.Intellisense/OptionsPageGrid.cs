using FSharp.Interactive.Intellisense.Lib;
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
        [Category("Settings")]
        [DisplayName("Autocomplete mode")]
        [Description("The way autocomplete is triggered (automatically, when Ctrl + Space is pressed, or Off).")]
        public AutocompleteModeType AutocompleteMode
        {
            get;
            set;
        }

        [Category("Settings")]
        [DisplayName("Intellisense provider")]
        [Description("Type of completions provider for Intellisense.")]
        public IntellisenseProviderType IntellisenseProvider
        {
            get;
            set;
        }
    }
}
