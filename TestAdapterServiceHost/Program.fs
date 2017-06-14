module R4nd0mApps.TddStud10.TestAdapterServiceHost.Program

open System
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.TestHost
open R4nd0mApps.TddStud10.TestRuntime
open System.Threading

[<EntryPoint>]
[<LoaderOptimization(LoaderOptimization.MultiDomain)>]
let main argv = 
    let ppid, uriShare = int argv.[0], ServiceDiscovery.fromString argv.[1]
    ppid
    |> ServiceHost.commitSucideOnParentExit
    |> Async.Start

    let covDataSvc = 
        ()
        |> CoverageDataCollector2Service
        |> ServiceFactory.CreateService<_, ICoverageDataCollector2>
    Environment.SetEnvironmentVariable(Marker.CoverageDataCollectorAddressEnvVarName, covDataSvc.Address.ToString())

    let testSvc = 
        covDataSvc.Service.GetCoverageData
        |> TestAdapterService
        |> ServiceFactory.CreateService<_, ITestAdapterService>

    ServiceDiscovery.shareUri uriShare testSvc.Address

    Thread.Sleep(Timeout.Infinite)

    0
