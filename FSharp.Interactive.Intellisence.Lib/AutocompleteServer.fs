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

    override x.Ping() = true
    override x.GetBaseDirectory() = System.AppDomain.CurrentDomain.BaseDirectory
    override x.GetCompletions(statement:String) = 
        let results = AutocompleteProvider.getCompletions(statement) 
                        |> Seq.map(fun e -> e.ToTuple())
                        |> Seq.toArray
        results


    static member ipcChannelName = "FSharp.Interactive.Intellisense.Lib"
    static member ipcChannelEndpoint = "AutocompleteService"

    static member StartServer(channelId : int) = 
        let channel = new IpcServerChannel(AutocompleteServer.ipcChannelName) // TODO: make it unique
        //Register the server channel.
        ChannelServices.RegisterChannel(channel, false)
        RemotingConfiguration.RegisterWellKnownServiceType(typeof<AutocompleteServer>, 
            (sprintf "%s_%d" AutocompleteServer.ipcChannelEndpoint channelId), 
            WellKnownObjectMode.Singleton)
        Debug.WriteLine(sprintf "Registered ipc://%s/%s_%d server endpoint" 
            AutocompleteServer.ipcChannelName AutocompleteServer.ipcChannelEndpoint channelId)
    
    static member StartClient(channelId: int) = 
        try 
            let channel = new IpcClientChannel()
            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(channel, false)
        with 
            | _ -> ()

        let channelUri = sprintf "ipc://%s/%s_%d" AutocompleteServer.ipcChannelName AutocompleteServer.ipcChannelEndpoint channelId
        //Register the client type.
        RemotingConfiguration.RegisterWellKnownClientType
            (typeof<AutocompleteServer>, channelUri)
        let T = 
            Activator.GetObject
                (typeof<AutocompleteServer>, channelUri)
        let x = T :?> AutocompleteService
        x
