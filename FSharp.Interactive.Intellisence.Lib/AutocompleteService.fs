namespace FSharp.Interactive.Intellisense.Lib

[<assembly: System.Runtime.InteropServices.ComVisible(false)>]
[<assembly: System.CLSCompliant(true)>]  
do()

open System
open System.Diagnostics
open System.Runtime.Remoting.Channels
open System.Runtime.Remoting
open System.Runtime.Remoting.Lifetime

[<AbstractClass>]
type internal FSharpInteractiveServer() =
    inherit System.MarshalByRefObject()  
    abstract Interrupt       : unit -> unit
#if FSI_SERVER_INTELLISENSE
    abstract Completions     : prefix:string -> string array
    abstract GetDeclarations : text:string * names:string array -> (string * string * string * int) array
#endif
    default x.Interrupt() = ()

    [<CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")>]
    static member StartServer(channelName:string,server:FSharpInteractiveServer) =
        let chan = new Ipc.IpcChannel(channelName) 
        LifetimeServices.LeaseTime            <- TimeSpan(7,0,0,0); // days,hours,mins,secs 
        LifetimeServices.LeaseManagerPollTime <- TimeSpan(7,0,0,0);
        LifetimeServices.RenewOnCallTime      <- TimeSpan(7,0,0,0);
        LifetimeServices.SponsorshipTimeout   <- TimeSpan(7,0,0,0);
        ChannelServices.RegisterChannel(chan,false);
        let objRef = RemotingServices.Marshal(server,"FSIServer") 
        ()

    static member StartClient(channelName) =
        let T = Activator.GetObject(typeof<FSharpInteractiveServer>,"ipc://" + channelName + "/FSIServer") 
        let x = T :?> FSharpInteractiveServer 
        x
