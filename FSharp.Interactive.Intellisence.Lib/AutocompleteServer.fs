namespace FSharp.Interactive.Intellisense.Lib

open System
open System.Reflection
open System.Diagnostics
open System.ServiceModel

(*
#r "C:\Users\Alexey\AppData\Local\Microsoft\VisualStudio\12.0Exp\Extensions\Aleksey Vlasenko\FSharp.Interactive.Intellisense\1.0\FSharp.Interactive.Intellisense.Lib.dll";;
FSharp.Interactive.Intellisense.Lib.AutocompleteServer.StartServer("FSharp.Interactive.Intellisense.Lib");;
open FSharp.Interactive.Intellisense.Lib;;
*)
[<ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, IncludeExceptionDetailInFaults = true)>]
type AutocompleteServer() = 

    interface AutocompleteService with
        member x.Ping() = true
        member x.GetBaseDirectory() = System.AppDomain.CurrentDomain.BaseDirectory
        member x.GetCompletions(prefix: String) (providerType: IntellisenseProviderType): seq<Completion> = 
            let results = AutocompleteProvider.getCompletions(prefix) 
            results 

    static member ipcAddressFormat = "net.pipe://localhost/Sharp.Interactive.Intellisense.Lib_{0}/AutocompleteService"

    static member StartServer(channelId : int) = 
        try
            let address = String.Format(AutocompleteServer.ipcAddressFormat, channelId)
            let serviceHost = new ServiceHost(typeof<AutocompleteServer>)
            let binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
            serviceHost.AddServiceEndpoint(typeof<AutocompleteService>, binding, address) |> ignore
            serviceHost.Open()
        with 
            | ex -> Debug.WriteLine(ex)
    
    static member StartClient(channelId: int) = 
        let address = String.Format(AutocompleteServer.ipcAddressFormat, channelId)
 
        let binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
        let ep = new EndpointAddress(address)
        let channel = ChannelFactory<AutocompleteService>.CreateChannel(binding, ep)
        channel