namespace FSharp.Interactive.Intellisense.Lib

module AutocompleteProvider = 

    open System
    open System.Collections.Generic
    open System.Reflection

    let getLastPartialSegment (statement:String) = 
        let lastStatmentDotIndex = statement.LastIndexOf('.')
        if lastStatmentDotIndex > 0 then
            statement.Substring(lastStatmentDotIndex + 1)
        else
            statement

    let getLastSegment (statement:String, line:String) = 
        let lastStatmentDotIndex = statement.LastIndexOf('.')
        let nextDotIndex = line.IndexOf('.', lastStatmentDotIndex + 1)
        if nextDotIndex > 0 then
            line.Substring(lastStatmentDotIndex + 1, nextDotIndex - (lastStatmentDotIndex + 1))
        else
            line.Substring(lastStatmentDotIndex + 1)

    let getTypeCompletionsForAssembly(statement:String, assembly:Assembly) : IEnumerable<String> =
        let assemblyTypeNames = assembly.GetTypes()
                                    |> Seq.filter(fun t -> t.IsPublic)
                                    |> Seq.map (fun t -> if t.FullName.LastIndexOf('`') > 0 then t.FullName.Remove(t.FullName.LastIndexOf('`')) else t.FullName) 
                                    |> Seq.filter(fun t -> t.StartsWith(statement))
                                    |> Seq.map(fun n -> getLastSegment(statement, n))
                                    |> Seq.distinct
        assemblyTypeNames

    let prefixesToRemoveRegex = ["get_"; "set_"; "add_"; "remove_"];
    let rec removePropertyPrefixRec (memberName:String) (prefixesToRemoveRegex:String list) =
        match prefixesToRemoveRegex with
        | head::tail -> if (memberName.StartsWith(head)) then memberName.Remove(0, head.Length) else removePropertyPrefixRec memberName tail
        | _ -> memberName
    let removePropertyPrefix (memberName:String) = removePropertyPrefixRec memberName prefixesToRemoveRegex

    let getFsiAssembly() = 
        System.AppDomain.CurrentDomain.GetAssemblies() 
        |> Seq.tryFind (fun assm -> assm.GetName().Name = "FSI-ASSEMBLY")

    let fAssembly = 
        System.AppDomain.CurrentDomain.GetAssemblies() 
        |> Seq.find (fun assm -> assm.GetName().Name = "Fsi")


    let getVariableNames(fsiAssembly:Assembly) =
        fsiAssembly.GetTypes()//FSI types have the name pattern FSI_####, where #### is the order in which they were created
        |> Seq.filter (fun ty -> ty.Name.StartsWith("FSI_"))
        |> Seq.sortBy (fun ty -> ty.Name.Split('_').[1] |> int)
        |> Seq.collect (fun ty ->
            let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
            ty.GetProperties(flags) 
            |> Seq.filter (fun pi -> pi.GetIndexParameters().Length > 0  |> not && pi.Name.Contains("@") |> not))
        |> Seq.map (fun pi -> pi.Name)
        |> Seq.distinct


    let getMethodNames(fsiAssembly:Assembly) =
        fsiAssembly.GetTypes()
        |> Seq.filter (fun ty -> ty.Name.StartsWith("FSI_")) 
        |> Seq.collect (fun ty ->
            let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
            ty.GetMethods(flags) 
            |> Seq.filter (fun pi -> pi.Name.Contains("@") |> not && pi.Name.StartsWith("get_") |> not && pi.Name.StartsWith("set_") |> not))
        |> Seq.map (fun pi -> pi.Name)
        |> Seq.distinct
        |> Seq.toList


    let getCompletionsForTypes(statement:String, fsiAssembly:Assembly) : seq<String> = seq {
        for assemblyName in fsiAssembly.GetReferencedAssemblies() do
            //printfn "%s" assemblyName.FullName
            let matches = AppDomain.CurrentDomain.GetAssemblies() 
                            |> Seq.filter(fun a -> a.GetName().FullName = assemblyName.FullName)
            if matches |> Seq.isEmpty |> not then
                let assembly = matches |> Seq.head
                let assemblyTypeNames = getTypeCompletionsForAssembly(statement, assembly) 
                yield! assemblyTypeNames

                if statement.LastIndexOf('.') > 0 then
                    let typeName = statement.Remove(statement.LastIndexOf('.'))
                    let type_ = assembly.GetType(typeName)
                    if not(type_ = null) then
                        let memberNames = type_.GetMembers(BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy ||| BindingFlags.Public)
                                            |> Seq.map(fun m -> removePropertyPrefix m.Name)
                                            |> Seq.filter (fun name -> name.StartsWith(getLastPartialSegment(statement))) 
                                            |> Seq.distinct
                        yield! memberNames
    }

    let getCompletions(statement:String) : seq<String> =
        seq {
            let fsiAssembly = getFsiAssembly()
            if fsiAssembly.IsSome then
                yield! getCompletionsForTypes(statement, fsiAssembly.Value)

                if statement.LastIndexOf('.') < 0 then
                    yield! getVariableNames(fsiAssembly.Value) |> Seq.filter(fun n -> n.StartsWith(statement))
                    yield! getMethodNames(fsiAssembly.Value) |> Seq.filter(fun n -> n.StartsWith(statement))
//                else 
//                    let variables = getVariableNames(fsiAssembly.Value) |> Seq.filter(fun n -> n.StartsWith(statement)) // Take first segment here?
//                    yield! variables // TODO take out methods and variables from this variable
        } 
        |> Seq.distinct
        |> Seq.sort




