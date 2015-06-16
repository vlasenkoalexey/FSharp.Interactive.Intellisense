namespace FSharp.Interactive.Intellisense.Lib

open System

[<Serializable>]
type public CompletionType = Namespace = 0 | Module = 1 | Class = 2 | Variable = 3 | Method = 4
type ICompletion = 
    abstract member Text : String
    abstract member CompletionType : CompletionType

[<Serializable>]
type public Completion(text:String, completionType:CompletionType) = 
    inherit System.MarshalByRefObject()
    
    member x.CompletionType = completionType
    member x.Text = text
    member x.ToTuple() = (text, int completionType)
    static member FromTuple (text:String, completionType:int) =
        new Completion(text, enum completionType)

    interface ICompletion with
        member x.CompletionType = completionType
        member x.Text = text

    interface IComparable<Completion> with
        member x.CompareTo other =
            text.CompareTo(other.Text) * 10 + x.CompletionType.CompareTo(other.CompletionType)
    interface IComparable with
        member x.CompareTo obj =
            match obj with
            | :? Completion as other -> (x :> IComparable<_>).CompareTo other
            | _                    -> invalidArg "obj" "not a Completion"

    interface IEquatable<Completion> with
        member x.Equals completion = (completion.Text = x.Text) && (completion.CompletionType = x.CompletionType)

    override x.Equals obj =
        match obj with
        | :? Completion as other -> (x :> IEquatable<_>).Equals other
        | _                    -> invalidArg "obj" "not a Category"
    override x.GetHashCode () =
        x.Text.GetHashCode()
        
