namespace R4nd0mApps.TddStud10.TestHost

open System.ServiceModel
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.XTestPlatform.Api

[<ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, 
                  IncludeExceptionDetailInFaults = true)>]
type TestAdapterService(getCoverageData) = 
    interface ITestAdapterService with
        
        member __.DiscoverTests rebasePaths (FilePath tdSearchPath) ignoredTestsPattern assemblyPath = 
            let cb = OperationContext.Current.GetCallbackChannel<ITestAdapterServiceCallback>()
            TestAdapterExtensions.discoverTestsCB rebasePaths tdSearchPath ignoredTestsPattern cb.OnTestDiscovered 
                assemblyPath
        
        member __.ExecuteTest (FilePath teSearchPath) testCase =
            testCase 
            |> DataContract.deserialize<XTestCase>  
            |> TestAdapterExtensions.executeTest teSearchPath
        member __.ExecuteTestsAndCollectCoverageData (FilePath teSearchPath) testCase = 
            { Result = TestAdapterExtensions.executeTest teSearchPath (DataContract.deserialize<XTestCase> testCase)
              CoverageData = getCoverageData() }
