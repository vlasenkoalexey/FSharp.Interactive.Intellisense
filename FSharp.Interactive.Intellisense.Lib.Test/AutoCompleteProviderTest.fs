namespace FSharp.Interactive.Intellisense.Lib.Test

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open FSharp.Interactive.Intellisense.Lib
open FsUnit.MsTest

[<TestClass>]
type UnitTest() = 
    [<TestMethod>]
    member x.getFirstSegment () = 
        AutocompleteProvider.getFirstSegment("foo.bar") |> should equal "foo"

    [<TestMethod>]
    member x.getLastPartialSegment () = 
        AutocompleteProvider.getLastPartialSegment("foo.bar.ba") |> should equal "ba"

    [<TestMethod>]
    member x.getLastSegment () = 
        AutocompleteProvider.getLastSegment("foo.bar.b", "foo.bar.baz") |> should equal "baz"

    [<TestMethod>]
    member x.getCompletionsForTypes () = 
        AutocompleteProvider.getCompletionsForTypes("Sys", typeof<String>.Assembly.GetTypes()) |> should contain "System"
        AutocompleteProvider.getCompletionsForTypes("System.", typeof<String>.Assembly.GetTypes()) |> should contain "Object"
        AutocompleteProvider.getCompletionsForTypes("System.D", typeof<String>.Assembly.GetTypes()) |> should contain "DateTime"
        AutocompleteProvider.getCompletionsForTypes("System.date", typeof<String>.Assembly.GetTypes()) |> should contain "DateTime"
        AutocompleteProvider.getCompletionsForTypes("Microsoft.FSharp.Core.Prin", typeof<Microsoft.FSharp.Core.unit>.Assembly.GetTypes()) |> should contain "Printf"

    [<TestMethod>]
    member x.removePropertyPrefix () = 
        AutocompleteProvider.removePropertyPrefix("get_Data") |> should equal "Data"

    [<TestMethod>]
    member x.getVariableNamesForType () = 
        AutocompleteProvider.getVariableNamesForType(typeof<String>) |> should contain "Length"

    [<TestMethod>]
    member x.getMethodNamesForType () = 
        AutocompleteProvider.getMethodNamesForType(typeof<Int32>) |> should contain "Parse"
        AutocompleteProvider.getMethodNamesForType(typeof<Int32>) |> should contain "ToString"

    [<TestMethod>]
    member x.getCompletionsForAssembly () = 
        let assembly = x.GetType().Assembly
        AutocompleteProvider.getCompletionsForAssembly("Sys", assembly) |> should contain "System"
        AutocompleteProvider.getCompletionsForAssembly("System.C", assembly) |> should contain "Console"
        AutocompleteProvider.getCompletionsForAssembly("System.con", assembly) |> should contain "Console"
        AutocompleteProvider.getCompletionsForAssembly("System.Console.W", assembly) |> should contain "WriteLine"

    [<TestMethod>]
    member x.getTypeCompletionsForReferencedAssemblies () = 
        AutocompleteProvider.getTypeCompletionsForReferencedAssemblies("Microsoft.FSharp.Core.Printf.p", x.GetType().Assembly) |> should contain "printfn"

    [<TestMethod>]
    member x.getCompletionsAndOpenDefaultNamespaces () = 
        AutocompleteProvider.getCompletionsAndOpenDefaultNamespaces("pr", x.GetType().Assembly) |> should contain "printfn"

    