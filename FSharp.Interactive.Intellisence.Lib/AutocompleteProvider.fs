namespace FSharp.Interactive.Intellisense.Lib

module AutocompleteProvider = 

    open System
    open System.Collections.Generic
    open System.Reflection
    //open FSharp.Interactive.Intellisense.Lib

    // Define your library scripting code here

    let getTypeCompletionsForAssembly(statement:String, assembly:Assembly) : IEnumerable<String> =
        let assemblyTypeNames = assembly.GetTypes()
                                    |> Seq.filter(fun t -> t.IsPublic)
                                    |> Seq.map (fun t -> if t.FullName.LastIndexOf('`') > 0 then t.FullName.Remove(t.FullName.LastIndexOf('`')) else t.FullName) 
                                    |> Seq.filter(fun t -> t.StartsWith(statement)) 
                                    |> Seq.distinct
        assemblyTypeNames

    let removePropertyPrefix (memberName:String) =
        if memberName.StartsWith("get_") || memberName.StartsWith("set_") then 
            memberName.Remove(0, "get_".Length)
        else
            memberName

    let getLastSegment (statement:String, line:String) = 
        let lastStatmentDotIndex = statement.LastIndexOf('.')
        let nextDotIndex = line.IndexOf('.', lastStatmentDotIndex + 1)
        if nextDotIndex > 0 then
            line.Substring(lastStatmentDotIndex + 1, nextDotIndex - (lastStatmentDotIndex + 1))
        else
            line.Substring(lastStatmentDotIndex + 1)

    let getCompletions(statement:String) : IEnumerable<String> =
        seq {
            let systemAssembly = Assembly.GetAssembly(typeof<Object>)
            let systemAssemblyTypeNames = getTypeCompletionsForAssembly(statement, systemAssembly) 
                                            |> Seq.map(fun n -> getLastSegment(statement, n))
            yield! systemAssemblyTypeNames

            if statement.LastIndexOf('.') > 0 then
                let type_ = systemAssembly.GetType(statement.Remove(statement.LastIndexOf('.')))
                if not(type_ = null) then
                    let memberNames = type_.GetMembers(BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy ||| BindingFlags.Public)
                                        |> Seq.map(fun m -> removePropertyPrefix m.Name)
                                        |> Seq.distinct
                    yield! memberNames
        
        }




