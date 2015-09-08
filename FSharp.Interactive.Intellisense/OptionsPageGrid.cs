using FSharp.Interactive.Intellisense.Lib;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSharp.Interactive.Intellisense
{
    [CLSCompliant(false), ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]  
    public class OptionsPageGrid : DialogPage
    {
        [Category(FSharp_Interactive_IntellisensePackage.SettingsCategoryName)]
        [DisplayName("Autocomplete mode")]
        [Description("The way autocomplete is triggered (automatically, when Ctrl + Space is pressed, or Off).")]
        public AutocompleteModeType AutocompleteMode
        {
            get;
            set;
        }

        [Category(FSharp_Interactive_IntellisensePackage.SettingsCategoryName)]
        [DisplayName("Intellisense provider")]
        [Description("Type of completions provider for Intellisense.")]
        public IntellisenseProviderType IntellisenseProvider
        {
            get;
            set;
        }
    }
}
