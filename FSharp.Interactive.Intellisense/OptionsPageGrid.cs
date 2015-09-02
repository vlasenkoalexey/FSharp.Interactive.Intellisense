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
        private int optionInt = 256;
        public enum AutocompleteMode
        {
            Automatic,
            CtrlSpace,
            Off
        }

        public enum IntellisenseProvider
        {
            Combined,
            FsiSession,
            Internal
        }

        [Category("Settings")]
        [DisplayName("Autocomplete mode")]
        [Description("The way autocomplete is triggered")]
        public int OptionInteger
        {
            get { return optionInt; }
            set { optionInt = value; }
        }
    }
}
