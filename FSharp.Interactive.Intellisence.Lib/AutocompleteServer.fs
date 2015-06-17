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
        let channel = new IpcServerChannel(sprintf "%s_%d"AutocompleteServer.ipcChannelName channelId)
        Debug.WriteLine(sprintf "Registering ipc://%s_%d/%s server endpoint" 
            AutocompleteServer.ipcChannelName channelId AutocompleteServer.ipcChannelEndpoint)
        //Register the server channel.
        ChannelServices.RegisterChannel(channel, false)
        RemotingConfiguration.RegisterWellKnownServiceType(typeof<AutocompleteServer>, 
            AutocompleteServer.ipcChannelEndpoint, 
            WellKnownObjectMode.Singleton)
    
    static member StartClient(channelId: int) = 
        Debug.WriteLine(sprintf "Registering ipc://%s_%d/%s client endpoint" 
            AutocompleteServer.ipcChannelName channelId AutocompleteServer.ipcChannelEndpoint)
        let channelUri = sprintf "ipc://%s_%d/%s" AutocompleteServer.ipcChannelName channelId AutocompleteServer.ipcChannelEndpoint
        try 
            let channel = new IpcClientChannel()
            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(channel, false)
                        //Register the client type.
            RemotingConfiguration.RegisterWellKnownClientType
                (typeof<AutocompleteServer>, channelUri)
        with 
            | _ -> ()


        let T = Activator.GetObject(typeof<AutocompleteServer>, channelUri)
        let x = T :?> AutocompleteService
        x
