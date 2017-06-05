namespace R4nd0mApps.TddStud10.TestHost

module TestAdapterExtensions = 
    open R4nd0mApps.TddStud10.Common
    open R4nd0mApps.TddStud10.Common.Domain
    open R4nd0mApps.TddStud10.TestExecution.Adapters
    open R4nd0mApps.XTestPlatform.Api
    open System.Collections.Concurrent
    
    let discoverTestsCB rebasePaths tdSearchPath ignoredTests cb assemblyPath = 
        let tds = tdSearchPath.ToString() |> AdapterLoader.LoadDiscoverers
        
        let discoveryCB (tc : XTestCase) = 
            let rebaseCFP cfp = 
                if not <| isNull cfp then 
                    PathBuilder.rebaseCodeFilePath rebasePaths (FilePath tc.CodeFilePath) |> Prelude.toStr
                else cfp
            { tc with CodeFilePath = rebaseCFP tc.CodeFilePath } |> cb
        
        let disc = XUnitTestDiscoverer()
        disc.TestDiscovered.Add(fun x -> SafeExec.safeExec (fun () -> discoveryCB x))
        disc.DiscoverTests(tds, assemblyPath, ignoredTests)
    
    let discoverTests rebasePaths tdSearchPath ignoredTests assemblyPath = 
        let dTests = ConcurrentBag<_>()
        discoverTestsCB rebasePaths tdSearchPath ignoredTests dTests.Add assemblyPath
        dTests :> seq<_>
    
    let executeTest teSearchPath (tc : XTestCase) = 
        let tes = teSearchPath.ToString() |> AdapterLoader.LoadExecutors
        let te = new XUnitTestExecutor()
        let trs = new ConcurrentQueue<_>()
        te.TestExecuted.Add(trs.Enqueue)
        te.ExecuteTests(tes, [ tc ])
        trs |> Seq.head
