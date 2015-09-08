using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FSharp.Interactive.Intellisense
{
    [CLSCompliant(false), ComVisible(true)]
    public enum AutocompleteModeType
    {
        Automatic,
        CtrlSpace,
        Off
    }
}
