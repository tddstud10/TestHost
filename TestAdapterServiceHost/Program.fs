module R4nd0mApps.TddStud10.TestAdapterServiceHost.Program

open System
open System.Diagnostics
open System.ServiceModel
open System.ServiceModel.Description
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.TestHost
open R4nd0mApps.TddStud10.TestRuntime

let private logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

// TODO: PARTHO: Consolidate the following two functions when we switch binging to tcp
let startCoverageDataService() = 
    logger.logInfof "TestAdapterService: Creating Coverage Data service."
    let service = CoverageDataCollector2Service()
    let serviceHost = new System.ServiceModel.ServiceHost(service)
    let binding = NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
    let address = sprintf "net.pipe://localhost/r4nd0mapps/tddstud10/CodeCoverageDataCollector/%d" <| Process.GetCurrentProcess().Id
    let endPoint = serviceHost.AddServiceEndpoint(typeof<ICoverageDataCollector2>, binding, address)
    if binding.GetType() <> typeof<NetNamedPipeBinding> then endPoint.ListenUriMode <- ListenUriMode.Unique
    serviceHost.Open()

    let epUri = serviceHost.ChannelDispatchers.[0].Listener.Uri
    logger.logInfof "TestAdapterService: Bound coverage data service to: %O" epUri
    serviceHost.Faulted.Add(logger.logErrorf "Service host faulted: %O")
    service, address

let startTestAdapterService uriShare getCoverageData =
    logger.logInfof "TestAdapterServiceHost: Creating Test Adapter service."
    let service = TestAdapterService(getCoverageData)
    let serviceHost = new ServiceHost(service)
    let binding = NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
    let address = sprintf "net.pipe://localhost/r4nd0mapps/tddstud10/TestAdapter/%d" <| Process.GetCurrentProcess().Id
    let endPoint = serviceHost.AddServiceEndpoint(typeof<ITestAdapterService>, binding, Uri(address))
    if binding.GetType() <> typeof<NetNamedPipeBinding> then endPoint.ListenUriMode <- ListenUriMode.Unique
    serviceHost.Open()

    let epUri = serviceHost.ChannelDispatchers.[0].Listener.Uri
    logger.logInfof "TestAdapterServiceHost: Bound Test Adapter service to: %O" epUri
    ServiceDiscovery.shareUri uriShare epUri
    serviceHost.Faulted.Add(logger.logErrorf "Service host faulted: %O")
    
[<EntryPoint>]
[<LoaderOptimization(LoaderOptimization.MultiDomain)>]
let main argv = 
    let ppid, uriShare = int argv.[0], ServiceDiscovery.fromString argv.[1]
    ppid
    |> ServiceHost.commitSucideOnParentExit
    |> Async.Start

    let covDataSvc, covDataSvcAddress = startCoverageDataService()
    Environment.SetEnvironmentVariable(Marker.CoverageDataCollectorAddressEnvVarName, covDataSvcAddress)
    startTestAdapterService uriShare covDataSvc.GetCoverageData

    Console.ReadLine() |> ignore

    0
