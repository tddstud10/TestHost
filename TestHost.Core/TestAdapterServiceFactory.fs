module R4nd0mApps.TddStud10.TestHost.TestAdapterServiceFactory

open R4nd0mApps.TddStud10.Common
open System
open System.Diagnostics
open System.IO
open System.ServiceModel

// TODO: PARTHO: Move to DFizer
let dfizePath path = 
    let newExtn = 
        sprintf "%s%s" (if DFizer.isDF() then ".DF"
                        else "") (Path.GetExtension path)
    (path, newExtn) |> Path.ChangeExtension

let create testDiscoveryCB = 
    let uriShare = ServiceDiscovery.createShare()
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger
    let sa = sprintf "%d %s" (Process.GetCurrentProcess().Id) (ServiceDiscovery.toString uriShare)
    
    let svcExePath = 
        [ Path.getLocalPath()
          "R4nd0mApps.TddStud10.TestAdapterServiceHost.exe" ]
        |> Path.combine
        |> dfizePath
    logger.logInfof "AdapterService Client: Starting Server: \"%s\" %s" svcExePath sa
    let proc = new Process()
    proc.StartInfo.FileName <- svcExePath
    proc.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
    proc.StartInfo.Arguments <- sa
    proc.Start() |> ignore
    logger.logInfof "AdapterService Client: Started Server with PID: %d" proc.Id
    let uri = uriShare |> ServiceDiscovery.readSharedUri
    logger.logInfof "AdapterService Client: Server listening at: %O" uri
    let svcCallback = 
        { new ITestAdapterServiceCallback with
              member __.OnTestDiscovered testCase = testCase |> testDiscoveryCB }
    
    let binding = NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
    let epa = EndpointAddress(uri)
    let svc = DuplexChannelFactory<ITestAdapterService>.CreateChannel(svcCallback, binding, epa)
    (svc :?> IContextChannel).OperationTimeout <- TimeSpan.FromHours(1.0)
    proc, svc
