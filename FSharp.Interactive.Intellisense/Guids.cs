// Guids.cs
// MUST match guids.h
using System;

namespace FSharp.Interactive.Intellisense
{
    static class GuidList
    {
        public const string guidFSharp_Interactive_IntellisensePkgString = "225a4dcf-86ef-4279-b906-f4155df19e5f";
        public const string guidFSharp_Interactive_IntellisenseCmdSetString = "18e0bce4-dfa9-4f39-a7bb-1998fdb3a0ee";

        public static readonly Guid guidFSharp_Interactive_IntellisenseCmdSet = new Guid(guidFSharp_Interactive_IntellisenseCmdSetString);
    };
}