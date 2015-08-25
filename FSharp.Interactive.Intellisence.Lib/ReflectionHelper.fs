namespace FSharp.Interactive.Intellisense.Lib

module ReflectionHelper = 

    open System
    open System.Reflection
    open Microsoft.FSharp.Reflection
    open System.Globalization

    let getField (fieldName:String) (obj:Object) =
        let flags = BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.GetField ||| BindingFlags.GetProperty
        let t = obj.GetType()
        t.GetField(fieldName, flags).GetValue(obj)

    let getProperty (fieldName:String) (obj:Object) =
        let flags = BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.GetField ||| BindingFlags.GetProperty
        let t = obj.GetType()
        t.GetProperty(fieldName, flags).GetValue(obj)

    let listMembers(obj:Object) =
        let flags = BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.GetField ||| BindingFlags.GetProperty
        obj.GetType().GetMembers(flags) 
        |> Seq.map(fun m->m.Name)
        |> List.ofSeq

    let invokeMethod (methodName:String) (parameters:Object[]) (obj:Object) =
        let flags = BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Public
        obj.GetType().GetMethod(methodName, flags).Invoke(obj, parameters)

    let fsiEvaluationSession = System.AppDomain.CurrentDomain 
                                |> getField "_unhandledException" 
                                |> getProperty "Target"
                                |> getField "callback"
                                |> getField "r"
                                |> getField "f"
                                |> getField "x"

    let fsiIntellisenseProvider = fsiEvaluationSession |> getField "fsiIntellisenseProvider"
    let istateRef = fsiEvaluationSession |> getField "istateRef"


    let prefix = "sup"
    fsiIntellisenseProvider |> invokeMethod "CompletionsForPartialLID" [|istateRef |> getProperty "contents"; prefix|]




