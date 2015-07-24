open System
open System.Reflection
open System.Linq
open System.Collections.Generic

let fsis = System.AppDomain.CurrentDomain.GetAssemblies() |> Seq.filter(fun a -> a.FullName.StartsWith("fsi")) |> Seq.toList ;;
let fsiAssembly = fsis.Head
//fsiAssembly.GetTypes() |> Seq.map(fun t -> t.FullName) |> Seq.filter(fun n -> n.Contains("ain")) |> Seq.skip (0) |> Seq.toList;;

let fsiEvaluationSessionType = fsiAssembly.GetType("Microsoft.FSharp.Compiler.Interactive.Shell+FsiEvaluationSession")

let getFsiEvaluationSessionMethod =  fsiEvaluationSessionType.GetMethod("GetFsiEvaluationSession", System.Reflection.BindingFlags.Static ||| System.Reflection.BindingFlags.Public ||| System.Reflection.BindingFlags.NonPublic)

let getValue (x:obj) =
   match x with
   | null -> None  // x is None
   | _ -> match x.GetType().GetProperty("Value") with
          | null -> None  // x is not an option
          | prop ->  
               let v = prop.GetValue( x, null )
               Some (v :?> Object)


let fsiEvaluationSessionOpt = getFsiEvaluationSessionMethod.Invoke(null, null)

let fsiEvaluationSession = getValue(fsiEvaluationSessionOpt).Value

let getCompletionsMethod = fsiEvaluationSessionType.GetMethod("GetCompletions", System.Reflection.BindingFlags.Static ||| System.Reflection.BindingFlags.Public ||| System.Reflection.BindingFlags.NonPublic)

let completions = getCompletionsMethod.Invoke(null, [| ("Sy" :> System.Object) |]);;


let app = typeof<System.Windows.Forms.Application>.GetMembers(BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.NonPublic)
let ctx = app |> Seq.map(fun m -> m.Name) |> Seq.filter(fun n -> n.Contains("Context")) |> Seq.toList

(*
System.Threading.Thread.CurrentThread.ExecutionContext
seq { for thread in System.Diagnostics.Process.GetCurrentProcess().Threads -> thread }
|> Seq.filter(fun f -> f.get)
*)

let threadContextMethod = typeof<System.Windows.Forms.Application>.GetMember("ThreadContext", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.NonPublic) |> Seq.head

typeof<System.Windows.Forms.Application>.GetType()


open Microsoft.FSharp.Reflection
open System.Reflection

let inline (?) (this : 'Source) (member' : string) (args : 'Args) : 'Result =
  let argArray =
    if box args = null then null
    elif FSharpType.IsTuple (args.GetType()) then
      FSharpValue.GetTupleFields args
    else [|args|]

  let flags = BindingFlags.GetProperty ||| BindingFlags.InvokeMethod
  this.GetType().InvokeMember(member', flags, null, this, argArray) :?> 'Result


let inline (?) (this : 'Source) (prop : string) : 'Result =
  let p = this.GetType().GetProperty(prop)
  p.GetValue(this, null) :?> 'Result