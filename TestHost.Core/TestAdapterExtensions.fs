namespace R4nd0mApps.TddStud10.TestHost

module TestAdapterExtensions = 
    open Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter
    open R4nd0mApps.TddStud10.Common
    open R4nd0mApps.TddStud10.Common.Domain
    open System
    open System.IO
    open System.Reflection
    open Microsoft.VisualStudio.TestPlatform.ObjectModel
    open System.Collections.Concurrent
    open R4nd0mApps.TddStud10.TestExecution
    open R4nd0mApps.TddStud10.TestExecution.Adapters
    
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

    let knownAdaptersMap = 
        [ (FilePath "xunit.runner.visualstudio.testadapter.dll", 
           [ ("ITestDiscoverer", "Xunit.Runner.VisualStudio.TestAdapter.VsTestRunner")
             ("ITestExecutor", "Xunit.Runner.VisualStudio.TestAdapter.VsTestRunner") ]
           |> Map.ofList)
          (FilePath "NUnit.VisualStudio.TestAdapter.dll", 
           [ ("ITestDiscoverer", "NUnit.VisualStudio.TestAdapter.NUnitTestDiscoverer")
             ("ITestExecutor", "NUnit.VisualStudio.TestAdapter.NUnitTestExecutor") ]
           |> Map.ofList)
          (FilePath "NUnit3.TestAdapter.dll", 
           [ ("ITestDiscoverer", "NUnit.VisualStudio.TestAdapter.NUnit3TestDiscoverer")
             ("ITestExecutor", "NUnit.VisualStudio.TestAdapter.NUnit3TestExecutor") ]
           |> Map.ofList) ]
        |> Map.ofList
    
    let createAdapter adapterMap adapterType path = 
        let loadAssembly (FilePath p) = Assembly.LoadFrom(p)
        
        let loadType a t =
            let asm = loadAssembly a
            t |> asm.GetType |> Option.ofNull 

        let asmResolver sp = 
            let innerFn _ (args : ResolveEventArgs) = 
                [ "*.dll"; "*.exe" ]
                |> Seq.map ((+) (AssemblyName(args.Name).Name))
                |> Seq.collect (fun name -> FilePath.enumerateFiles SearchOption.AllDirectories name sp)
                |> Seq.tryFind FilePath.fileExists
                |> Option.fold (fun _ -> loadAssembly) null
            innerFn
        
        let resolver = asmResolver <| FilePath.getDirectoryName path
        try 
            logger.logInfof "Attempting to load Test Adapter %s from %O" adapterType path
            AppDomain.CurrentDomain.add_AssemblyResolve (ResolveEventHandler resolver)
            let res =
                adapterMap
                |> Map.tryFind (FilePath.getFileName path)
                |> Option.bind (Map.tryFind adapterType)
                |> Option.bind (loadType path)
                |> Option.bind (Activator.CreateInstance >> Some)
            logger.logInfof "Loaded Test Adapter %s from %O" adapterType path
            res
        finally
            AppDomain.CurrentDomain.remove_AssemblyResolve (ResolveEventHandler resolver)
    
    let findAdapterAssemblies dir = 
        if FilePath.directoryExists dir then 
            FilePath.enumerateFiles SearchOption.AllDirectories "*.testadapter.dll" dir
        else Seq.empty<FilePath>
    
    let findTestDiscoverers adapterMap = 
        findAdapterAssemblies
        >> Seq.choose (createAdapter adapterMap "ITestDiscoverer")
        >> Seq.map (fun a -> a :?> ITestDiscoverer)
    
    let findTestExecutors adapterMap = 
        findAdapterAssemblies
        >> Seq.choose (createAdapter adapterMap "ITestExecutor")
        >> Seq.map (fun a -> a :?> ITestExecutor)

    let discoverTestsCB rebasePaths tdSearchPath ignoredTests cb assemblyPath = 
        let tds =
            tdSearchPath
            |> findTestDiscoverers knownAdaptersMap
    
        let discoveryCB (tc : TestCase) = 
            if not <| isNull tc.CodeFilePath then
                let cfp = PathBuilder.rebaseCodeFilePath rebasePaths (FilePath tc.CodeFilePath)
                tc.CodeFilePath <- cfp.ToString()
            tc
            |> TestPlatformExtensions.toDTestCase2
            |> cb

        let disc = XUnitTestDiscoverer()
        disc.TestDiscovered.Add(fun x -> SafeExec.safeExec (fun () -> discoveryCB x))
        disc.DiscoverTests(tds, assemblyPath, ignoredTests)

    let discoverTests rebasePaths tdSearchPath ignoredTests assemblyPath = 
        let dTests = ConcurrentBag<DTestCase2>()
        discoverTestsCB rebasePaths tdSearchPath ignoredTests dTests.Add assemblyPath
        dTests :> seq<_>

    let executeTest teSearchPath (tc : DTestCase2) =
        let tes =
            teSearchPath
            |> findTestExecutors knownAdaptersMap

        let te = new XUnitTestExecutor()
        let trs = new ConcurrentQueue<DTestResult>()
        te.TestExecuted.Add(trs.Enqueue)

        let tc = tc.TestCase |> DataContract.deserialize<TestCase>
        te.ExecuteTests(tes, [ tc ])
        trs |> Seq.head
