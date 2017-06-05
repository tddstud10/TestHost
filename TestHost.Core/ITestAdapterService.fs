namespace R4nd0mApps.TddStud10.TestHost

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.XTestPlatform.Api
open System.ServiceModel

type ITestAdapterServiceCallback = 
    [<OperationContract>]
    abstract OnTestDiscovered : testCase:XTestCase -> unit

[<ServiceContract(Namespace = "https://www.tddstud10.r4nd0mapps.com", 
                  CallbackContract = typeof<ITestAdapterServiceCallback>)>]
type ITestAdapterService = 
    
    [<OperationContract>]
    abstract DiscoverTests : basePaths:(FilePath * FilePath)
     -> tdSearchPath:FilePath -> ignoredTestsPattern:string [] -> assemblyPath:FilePath -> unit
    
    [<OperationContract>]
    abstract ExecuteTest : teSearchPath:FilePath -> testCase:XTestCase -> XTestResult
    
    [<OperationContract>]
    abstract ExecuteTestsAndCollectCoverageData : teSearchPath:FilePath
     -> testCase:XTestCase -> DTestResultWithCoverageData
