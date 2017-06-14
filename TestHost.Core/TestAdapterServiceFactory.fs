module R4nd0mApps.TddStud10.TestHost.TestAdapterServiceFactory

open R4nd0mApps.TddStud10.Common
open System
open System.Diagnostics
open System.ServiceModel
open System.ServiceModel.Channels
open R4nd0mApps.XTestPlatform.Api
open R4nd0mApps.TddStud10.TestRuntime

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
    let disp = 
        { new IDisposable with
            member x.Dispose() = () |> proc.Kill |> proc.Dispose }
    logger.logInfof "AdapterService Client: Started Server with PID: %d" proc.Id
    let uri = uriShare |> ServiceDiscovery.readSharedUri
    logger.logInfof "AdapterService Client: Server listening at: %O" uri
    let binding = 
        if Marker.OnMono then NetTcpBinding() :> Binding
        else NetNamedPipeBinding(NetNamedPipeSecurityMode.None) :> _
    let epa = EndpointAddress(uri)
    let svc = DuplexChannelFactory<ITestAdapterService>.CreateChannel(svcCV, binding, epa)
    (svc :?> IContextChannel).OperationTimeout <- TimeSpan.FromHours(1.0)
    disp, svc
