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
    [<OperationContract>]
    abstract GetCompletions: prefix:String -> providerType:IntellisenseProviderType -> seq<Completion>