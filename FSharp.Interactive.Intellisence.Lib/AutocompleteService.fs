namespace FSharp.Interactive.Intellisense.Lib

open System
open System.Diagnostics
open System.Runtime.Remoting.Channels
open System.Runtime.Remoting
open System.Runtime.Remoting.Lifetime

[<AbstractClass>]
type AutocompleteService() =
    inherit System.MarshalByRefObject()  
    abstract Test       : unit -> int
    abstract GetBaseDirectory : unit -> String
