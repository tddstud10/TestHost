namespace R4nd0mApps.TddStud10.TestHost

open System.ServiceModel

[<ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, 
                  IncludeExceptionDetailInFaults = true)>]
type TestAdapterService(getCoverageData) = 
    interface ITestAdapterService with
        
        member __.DiscoverTests rebasePaths tdSearchPath ignoredTestsPattern assemblyPath = 
            let cb = OperationContext.Current.GetCallbackChannel<ITestAdapterServiceCallback>()
            TestAdapterExtensions.discoverTestsCB rebasePaths tdSearchPath ignoredTestsPattern cb.OnTestDiscovered 
                assemblyPath
        
        member __.ExecuteTest teSearchPath testCase = testCase |> TestAdapterExtensions.executeTest teSearchPath
        member __.ExecuteTestsAndCollectCoverageData teSearchPath testCase = 
            { Result = TestAdapterExtensions.executeTest teSearchPath testCase
              CoverageData = getCoverageData() }
