namespace FSharp.Interactive.Intellisense.Lib

open System
open System.Diagnostics
open System.Runtime.Remoting.Channels
open System.Runtime.Remoting
open System.Runtime.Remoting.Lifetime

[<AbstractClass>]
[<Serializable>]
type AutocompleteService() = 
    inherit System.MarshalByRefObject()
    abstract Ping : unit -> bool
    abstract GetBaseDirectory : unit -> String
    // If I use any type from this assembly, Remoting crashes with "unable to find assembly" exception.
    abstract GetCompletions: String -> (String * int)[] 