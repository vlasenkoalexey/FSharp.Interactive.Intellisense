namespace FSharp.Interactive.Intellisense.Lib

open System
open System.Diagnostics
open System.Runtime.Remoting.Channels
open System.Runtime.Remoting
open System.Runtime.Remoting.Lifetime
open System.ServiceModel

[<Serializable>]
[<ServiceContract>]
type AutocompleteService = 
    [<OperationContract>]
    abstract Ping : a:unit -> bool
    [<OperationContract>]
    abstract GetBaseDirectory : b:unit -> String
    // If I use any type from this assembly, Remoting crashes with "unable to find assembly" exception.
    [<OperationContract>]
    abstract GetCompletions: c:String -> (String * int)[] 