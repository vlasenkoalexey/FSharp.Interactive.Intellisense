namespace FSharp.Interactive.Intellisense.Lib

open System
open System.Diagnostics
open System.ServiceModel
open System.Collections.Generic

[<Serializable>]
[<ServiceContract>]
type AutocompleteService = 
    [<OperationContract>]
    abstract Ping : a:unit -> bool
    [<OperationContract>]
    abstract GetBaseDirectory : b:unit -> String
    // If I use any type from this assembly, Remoting crashes with "unable to find assembly" exception.
    [<OperationContract>]
    abstract GetCompletions: c:String -> seq<Completion>