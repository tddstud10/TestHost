module R4nd0mApps.TddStud10.TestAdapterServiceHost.Program

open System
open System.Diagnostics
open System.ServiceModel
open System.ServiceModel.Description
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.TestHost

let private logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

type IMarker = interface end

[<EntryPoint>]
[<LoaderOptimization(LoaderOptimization.MultiDomain)>]
let main argv = 
    let ppid, uriShare = int argv.[0], ServiceDiscovery.fromString argv.[1]
    ppid
    |> ServiceHost.commitSucideOnParentExit
    |> Async.Start

    logger.logInfof "TestAdapterServiceHost: Createing Service Host"
    let service = TestAdapterService()
    let serviceHost = new ServiceHost(service)
    let binding = NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
    let address = sprintf "net.pipe://localhost/r4nd0mapps/tddstud10/TestAdapter/%d" <| Process.GetCurrentProcess().Id
    let endPoint = serviceHost.AddServiceEndpoint(typeof<ITestAdapterService>, binding, Uri(address))
    if binding.GetType() <> typeof<NetNamedPipeBinding> then endPoint.ListenUriMode <- ListenUriMode.Unique
    serviceHost.Open()

    // Share entire binding info
    let epUri = serviceHost.ChannelDispatchers.[0].Listener.Uri
    logger.logInfof "TestAdapterServiceHost: Bound service endpoint to: %O" epUri
    ServiceDiscovery.shareUri uriShare epUri
    serviceHost.Faulted.Add(logger.logErrorf "Service host faulted: %O")


    Console.ReadLine() |> ignore

    0
