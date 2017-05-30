module R4nd0mApps.TddStud10.TestHost.ContractTests

open global.Xunit
open FsUnit.Xunit
open R4nd0mApps.TddStud10.TestAdapterServiceHost.Program
open System.Diagnostics
open System.ServiceModel
open System.Collections.Concurrent
open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open System
open System.IO
open R4nd0mApps.TddStud10.TestExecution

[<Fact>]
let ``Adapter service can discover and run tests`` () = 
    let uriShare = ServiceDiscovery.createShare ()

    let sp =    
        typeof<R4nd0mApps.TddStud10.TestAdapterServiceHost.Program.IMarker>
        |> Path.getAssemblyPath
    
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger
    
    let sa = sprintf "%d %s" (Process.GetCurrentProcess().Id) (ServiceDiscovery.toString uriShare)
    logger.logInfof "AdapterService Client: Starting Server: \"%s\" %s" sp sa
    let proc = new Process()
    proc.StartInfo.FileName <- sp
    proc.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
    proc.StartInfo.Arguments <- sa
    proc.Start() |> ignore
    logger.logInfof "AdapterService Client: Started Server with PID: %d" proc.Id
    let uri = uriShare |> ServiceDiscovery.readSharedUri
    logger.logInfof "AdapterService Client: Server listening at: %O" uri

    let testCases = ConcurrentBag<DTestCase2>()
    let serviceCB = 
        { new ITestAdapterServiceCallback with
            member __.OnTestDiscovered testCase = 
                testCase |> testCases.Add }

    let binding = NetNamedPipeBinding(NetNamedPipeSecurityMode.None)
    let epa = EndpointAddress(uri)
    let ret = DuplexChannelFactory<ITestAdapterService>.CreateChannel(serviceCB, binding, epa)
    (ret :?> IContextChannel).OperationTimeout <- TimeSpan.FromHours(1.0)
    
    let adapterSearchPath = TestPlatformExtensions.getLocalPath()
    let assemblyPath = (Path.getLocalPath(), @"TestData2\CSXUnit1xNUnit3x.dll") |> Path.Combine |> FilePath
    let rebasePaths = FilePath @"D:\src\t\Engine\TestProjects\CSXUnit1xNUnit3x.NET20\Sln.sln", FilePath @"D:\delme\_tdd\CSXUnit1xNUnit3x.NET20\Sln.sln"
    ret.DiscoverTests rebasePaths adapterSearchPath [||] assemblyPath

    let results =
        testCases
        |> Seq.map (fun tc ->
                    let x = tc |> ret.ExecuteTests adapterSearchPath
                    x.DisplayName, x.Outcome)
        |> Seq.sortBy fst
        |> Seq.toArray

    proc.Kill()
        
    results |> should equal [| """CSXUnit1xNUnit3x.StringTests3.IndexOf(input: "hello world", letter: 'w', expected: 6)""", TOPassed
                               """CSXUnit1xNUnit3x.StringTests3.TestToSkip""", TOPassed |]

