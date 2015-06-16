namespace FSharp.Interactive.Intellisense.Lib

module AutocompleteProvider = 

    open System
    open System.Collections.Generic
    open System.Reflection
    open Microsoft.FSharp.Reflection

    // Returns first segment foo.bar -> foo
    let getFirstSegment(statement:String) =
        let firstStatementDotIndex = statement.IndexOf('.')
        if firstStatementDotIndex > 0 then
            statement.Remove(firstStatementDotIndex)
        else
            statement

    // Returns last segment: foo.bar.ba -> ba
    let getLastPartialSegment (statement:String) = 
        let lastStatmentDotIndex = statement.LastIndexOf('.')
        if lastStatmentDotIndex > 0 then
            statement.Substring(lastStatmentDotIndex + 1)
        else
            statement

    // Returns last completed segment foo.bar.baz, foo.bar.b -> baz
    let getLastSegment (statement:String, line:String) = 
        let lastStatmentDotIndex = statement.LastIndexOf('.')
        let nextDotIndex = line.IndexOf('.', lastStatmentDotIndex + 1)
        if nextDotIndex > 0 then
            line.Substring(lastStatmentDotIndex + 1, nextDotIndex - (lastStatmentDotIndex + 1))
        else
            line.Substring(lastStatmentDotIndex + 1)

    let getCompletionsForTypes(statement:String, types:seq<Type>) : IEnumerable<String> =
        let assemblyTypeNames = 
            types
            |> Seq.filter(fun t -> t.IsPublic)
            |> Seq.map (fun t -> if t.FullName.LastIndexOf('`') > 0 then t.FullName.Remove(t.FullName.LastIndexOf('`')) else 
                                    if FSharpType.IsModule(t) then t.FullName.Remove(t.FullName.Length - "Module".Length) else t.FullName) 
            |> Seq.filter(fun t -> t.StartsWith(statement, StringComparison.OrdinalIgnoreCase))
            |> Seq.map(fun n -> getLastSegment(statement, n))
            |> Seq.distinct
        assemblyTypeNames

    let rec removePropertyPrefixRec (memberName:String) (prefixesToRemoveRegex:String list) =
        match prefixesToRemoveRegex with
        | head::tail -> if (memberName.StartsWith(head)) then memberName.Remove(0, head.Length) else removePropertyPrefixRec memberName tail
        | _ -> memberName
    let removePropertyPrefix (memberName:String) = 
        let prefixesToRemoveRegex = ["get_"; "set_"; "add_"; "remove_"];
        removePropertyPrefixRec memberName prefixesToRemoveRegex

    let getFsiAssembly() = 
        System.AppDomain.CurrentDomain.GetAssemblies() 
        |> Seq.tryFind (fun assm -> assm.GetName().Name = "FSI-ASSEMBLY")

    let fAssembly = 
        System.AppDomain.CurrentDomain.GetAssemblies() 
        |> Seq.find (fun assm -> assm.GetName().Name = "Fsi")

    let getPropertyInfosForType(t:Type) = 
        let flags = BindingFlags.Static ||| BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy 
        t.GetProperties(flags) 
        |> Seq.filter (fun pi -> pi.IsSpecialName |> not && pi.GetIndexParameters().Length > 0  |> not && pi.Name.Contains("@") |> not)

    let getVariableNames(fsiAssembly:Assembly) =
        fsiAssembly.GetTypes()//FSI types have the name pattern FSI_####, where #### is the order in which they were created
        |> Seq.filter (fun ty -> ty.Name.StartsWith("FSI_"))
        |> Seq.sortBy (fun ty -> ty.Name.Split('_').[1] |> int)
        |> Seq.collect (fun ty -> getPropertyInfosForType(ty))
        |> Seq.map (fun pi -> pi.Name)
        |> Seq.distinct

    let getVariableTypeByName(fsiAssembly:Assembly, name:String) =
        fsiAssembly.GetTypes()//FSI types have the name pattern FSI_####, where #### is the order in which they were created
        |> Seq.filter (fun ty -> ty.Name.StartsWith("FSI_"))
        |> Seq.sortBy (fun ty -> ty.Name.Split('_').[1] |> int)
        |> Seq.collect (fun ty -> getPropertyInfosForType(ty))
        |> Seq.tryFind (fun pi -> pi.Name.Equals(name, StringComparison.Ordinal))
        |> Option.map (fun pi -> pi.PropertyType)

    let getVariableNamesForType(t:Type) = 
        getPropertyInfosForType(t)
        |> Seq.map (fun pi -> pi.Name)
        |> Seq.distinct

    let getMethodInfosForType(t:Type) =
        let flags = BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy ||| BindingFlags.Instance
        t.GetMethods(flags) 
        |> Seq.filter (fun pi -> pi.IsSpecialName |> not && pi.Name.Contains("@") |> not && pi.Name.StartsWith("get_") |> not && pi.Name.StartsWith("set_") |> not)

    let getMethodNames(fsiAssembly:Assembly) =
        fsiAssembly.GetTypes()
        |> Seq.filter (fun ty -> ty.Name.StartsWith("FSI_")) 
        |> Seq.collect (fun ty -> getMethodInfosForType(ty))
        |> Seq.map (fun mi -> mi.Name)
        |> Seq.distinct

    let methodSourceName (mi:MemberInfo) =
        mi.GetCustomAttributes(true)
        |> Array.tryPick 
                (function
                    | :? CompilationSourceNameAttribute as csna -> Some(csna)
                    | _ -> None)
        |> (function | Some(csna) -> csna.SourceName | None -> mi.Name)

    let getMethodNamesForType(t:Type) =
        getMethodInfosForType(t)
        |> Seq.map (fun mi -> if FSharpType.IsModule(t) then methodSourceName(mi) else mi.Name)
        |> Seq.distinct

    let getTypeCompletionsForReferencedAssemblies(statement:String, fsiAssembly:Assembly) : seq<String> = seq {
        for assemblyName in fsiAssembly.GetReferencedAssemblies() do
            //printfn "%s" assemblyName.FullName
            let matches = AppDomain.CurrentDomain.GetAssemblies() 
                            |> Seq.filter(fun a -> a.GetName().FullName = assemblyName.FullName)
            if matches |> Seq.isEmpty |> not then
                let assembly = matches |> Seq.head
                let assemblyTypeNames = getCompletionsForTypes(statement, assembly.GetTypes()) 
                yield! assemblyTypeNames

                if statement.LastIndexOf('.') > 0 then
                    let typeName = statement.Remove(statement.LastIndexOf('.'))
                    let type_ = if assembly.GetType(typeName) = null then assembly.GetType(typeName + "Module") else assembly.GetType(typeName)
                    if not(type_ = null) then
                        let memberNames = getMethodNamesForType(type_)
                        yield! memberNames
    }

    let getCompletionsForAssembly(statement:String, fsiAssembly:Assembly) =
        seq {
            yield! getTypeCompletionsForReferencedAssemblies(statement, fsiAssembly)

            if statement.LastIndexOf('.') < 0 then
                yield! getVariableNames(fsiAssembly) |> Seq.filter(fun n -> n.StartsWith(statement))
                // yield! getMethodNames(fsiAssembly) |> Seq.filter(fun n -> n.StartsWith(statement)) <- no method names since no variable is in the statement.
            else 
                let variables = getVariableNames(fsiAssembly) |> Seq.filter(fun n -> n.StartsWith(getFirstSegment(statement), StringComparison.OrdinalIgnoreCase))
                if variables |> Seq.isEmpty then
                    yield! variables // TODO take out methods and variables from this variable
                else
                    let variable = variables |> Seq.head
                    let typeOpt = getVariableTypeByName(fsiAssembly, variable)
                    if typeOpt.IsSome then
                        let noFirstSegmentStatement = statement.Remove(0, variable.Length + 1)
                        yield! getVariableNamesForType(typeOpt.Value) |> Seq.filter(fun n -> n.StartsWith(noFirstSegmentStatement, StringComparison.OrdinalIgnoreCase))
                        yield! getMethodNamesForType(typeOpt.Value) |> Seq.filter(fun n -> n.StartsWith(noFirstSegmentStatement, StringComparison.OrdinalIgnoreCase))
        } 
        |> Seq.distinct
        |> Seq.sort

    let getCompletions(statement:String) : seq<String> = 
        seq {
            let fsiAssemblyOpt = getFsiAssembly()
            if fsiAssemblyOpt.IsSome then
                yield! getCompletionsForAssembly(statement, fsiAssemblyOpt.Value)
                yield! getCompletionsForAssembly("Microsoft.FSharp.Collections." + statement, fsiAssemblyOpt.Value)
                yield! getCompletionsForAssembly("Microsoft.FSharp.Core.Printf." + statement, fsiAssemblyOpt.Value)
        }
        |> Seq.distinct
        |> Seq.sort
    
