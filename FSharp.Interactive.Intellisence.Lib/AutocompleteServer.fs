namespace FSharp.Interactive.Intellisense.Lib

open System.Runtime.Remoting.Channels
open System.Runtime.Remoting
open System.Runtime.Remoting.Channels.Ipc
open System
open System.Reflection
open System.Diagnostics

(*
#r "C:\Users\Alexey\AppData\Local\Microsoft\VisualStudio\12.0Exp\Extensions\Aleksey Vlasenko\FSharp.Interactive.Intellisense\1.0\FSharp.Interactive.Intellisense.Lib.dll";;
FSharp.Interactive.Intellisense.Lib.AutocompleteServer.StartServer("FSharp.Interactive.Intellisense.Lib");;
open FSharp.Interactive.Intellisense.Lib;;
*)
type AutocompleteServer() = 
    inherit AutocompleteService()

    do AppDomain.CurrentDomain.add_AssemblyResolve(fun obj arg -> Debug.WriteLine "Resolving FSharp.Interactive.Intellisense.dll"; typeof<AutocompleteServer>.Assembly)

    override x.Test() = 5
    override x.GetBaseDirectory() = System.AppDomain.CurrentDomain.BaseDirectory
    override x.GetCompletions(statement:String) = 
        //Debugger.Break()
        let results = AutocompleteProvider.getCompletions(statement) |> Seq.toArray
        results
    
    static member StartServer(channelName : string) = 
        let channel = new IpcServerChannel("FSharp.Interactive.Intellisense.Lib") // TODO: make it unique
        //Register the server channel.
        ChannelServices.RegisterChannel(channel, false)
        Debug.WriteLine("Registered FSharp.Interactive.Intellisense.Lib channel")
        RemotingConfiguration.RegisterWellKnownServiceType
            (typeof<AutocompleteServer>, "AutocompleteService", WellKnownObjectMode.Singleton)
    
    static member StartClient(channelName) = 
        try 
            let channel = new IpcClientChannel()
            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(channel, false)
        with 
            | _ -> ()

        //Register the client type.
        RemotingConfiguration.RegisterWellKnownClientType
            (typeof<AutocompleteServer>, "ipc://FSharp.Interactive.Intellisense.Lib/AutocompleteService")
        //let T = Activator.GetObject(typeof<AutocompleteService>,"ipc://" + channelName + "/AutocompleteService") 
        let T = 
            Activator.GetObject
                (typeof<AutocompleteService>, "ipc://FSharp.Interactive.Intellisense.Lib/AutocompleteService")
        let x = T :?> AutocompleteService
        x
