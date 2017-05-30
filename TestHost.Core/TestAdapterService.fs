namespace R4nd0mApps.TddStud10.TestHost

open System.ServiceModel

[<ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)>]
type TestAdapterService() = 
    interface ITestAdapterService with
        
        member __.DiscoverTests rebasePaths tdSearchPath ignoredTestsPattern assemblyPath = 
            let cb = OperationContext.Current.GetCallbackChannel<ITestAdapterServiceCallback>()
            TestAdapterExtensions.discoverTestsCB rebasePaths tdSearchPath ignoredTestsPattern cb.OnTestDiscovered 
                assemblyPath
        
        member __.ExecuteTests teSearchPath testCase = 
            Seq.singleton testCase
            |> TestAdapterExtensions.executeTest teSearchPath
            |> Seq.head
        
        member __.ExecuteTestsAndCollectCoverageData teSearchPath testCase = Prelude.undefined
