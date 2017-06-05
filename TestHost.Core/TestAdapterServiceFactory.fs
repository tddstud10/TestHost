module R4nd0mApps.TddStud10.TestHost.TestAdapterServiceFactory

open R4nd0mApps.TddStud10.Common
open System
open System.Diagnostics
open System.ServiceModel
open R4nd0mApps.XTestPlatform.Api

type TestAdapterServiceCallback() as it = 
    member val Callback = ignore with get, set
    interface ITestAdapterServiceCallback with
        member __.OnTestDiscovered(testCase : XTestCase) : unit = testCase |> it.Callback

let create (svcCV : ITestAdapterServiceCallback) = 
    let uriShare = ServiceDiscovery.createShare()
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger
    let sa = sprintf "%d %s" (Process.GetCurrentProcess().Id) (ServiceDiscovery.toString uriShare)
    
    let svcExePath = 
        [ Path.getLocalPath()
          "R4nd0mApps.TddStud10.TestAdapterServiceHost.exe" ]
        |> Path.combine
        |> DFizer.dfizePath
    logger.logInfof "AdapterService Client: Starting Server: \"%s\" %s" svcExePath sa
    let proc = new Process()
    proc.StartInfo.FileName <- svcExePath
    proc.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
    proc.StartInfo.Arguments <- sa
    proc.Start() |> ignore
    logger.logInfof "AdapterService Client: Started Server with PID: %d" proc.Id
    let uri = uriShare |> ServiceDiscovery.readSharedUri
    logger.logInfof "AdapterService Client: Server listening at: %O" uri
    let binding = NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
    let epa = EndpointAddress(uri)
    let svc = DuplexChannelFactory<ITestAdapterService>.CreateChannel(svcCV, binding, epa)
    (svc :?> IContextChannel).OperationTimeout <- TimeSpan.FromHours(1.0)
    proc, svc
