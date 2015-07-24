open System
open System.Reflection
open Microsoft.FSharp.Reflection
open System.Globalization

[<AutoOpen>]
module Reflection =   
      // Various flags configurations for Reflection
      let staticFlags = BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.GetField
      let instanceFlags = BindingFlags.NonPublic ||| BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.GetField
      let ctorFlags = instanceFlags
      let inline asMethodBase(a:#MethodBase) = a :> MethodBase
  
      let (?) (o:obj) name : 'R =
        // The return type is a function, which means that we want to invoke a method
        if FSharpType.IsFunction(typeof<'R>) then
          let argType, resType = FSharpType.GetFunctionElements(typeof<'R>)
          FSharpValue.MakeFunction(typeof<'R>, fun args ->
            // We treat elements of a tuple passed as argument as a list of arguments
            // When the 'o' object is 'System.Type', we call static methods
            let methods, instance, args = 
              let args = 
                if argType = typeof<unit> then [| |]
                elif not(FSharpType.IsTuple(argType)) then [| args |]
                else FSharpValue.GetTupleFields(args)
              if (typeof<System.Type>).IsAssignableFrom(o.GetType()) then 
                let methods = (unbox<Type> o).GetMethods(staticFlags) |> Array.map asMethodBase
                let ctors = (unbox<Type> o).GetConstructors(ctorFlags) |> Array.map asMethodBase
                Array.concat [ methods; ctors ], null, args
              else 
                o.GetType().GetMethods(instanceFlags) |> Array.map asMethodBase, o, args
        
            // A simple overload resolution based on the name and number of parameters only
            let methods = 
              [ for m in methods do
                  if m.Name = name && m.GetParameters().Length = args.Length then yield m ]
            match methods with 
            | [] -> failwithf "No method '%s' with %d arguments found" name args.Length
            | _::_::_ -> failwithf "Multiple methods '%s' with %d arguments found" name args.Length
            | [:? ConstructorInfo as c] -> c.Invoke(args)
            | [ m ] -> m.Invoke(instance, args) ) |> unbox<'R>
        else
          // When the 'o' object is 'System.Type', we access static properties
          let typ, flags, instance = 
            if (typeof<System.Type>).IsAssignableFrom(o.GetType()) then unbox o, staticFlags, null
            else o.GetType(), instanceFlags, o
      
          // Find a property that we can call and get the value
          let prop = typ.GetProperty(name, flags)
          if prop = null then failwithf "Property '%s' not found in '%s' using flags '%A'." name typ.Name flags
          let meth = prop.GetGetMethod(true)
          if meth = null then failwithf "Property '%s' found, but doesn't have 'get' method." name
          let field = typ.GetField(name, flags)
          meth.Invoke(instance, [| |]) |> unbox<'R>

let (?/) (this : 'Source) (prop : string) : 'Result =
    let flags = 
                BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.GetField
    let p = this.GetType().GetField(prop, flags)
    p.GetValue(this) :?> 'Result

let getFieldGen<'T> (fieldName:String) (obj:Object) =
    let flags = BindingFlags.NonPublic ||| BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.GetField ||| BindingFlags.GetProperty
    let t = obj.GetType()
    t.GetField(fieldName, flags).GetValue(obj) :?> 'T

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







