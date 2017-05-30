namespace R4nd0mApps.TddStud10.TestHost

open R4nd0mApps.TddStud10.Common.Domain
open System.ServiceModel

type ITestAdapterServiceCallback = 
    [<OperationContract>]
    abstract OnTestDiscovered : testCase:DTestCase2 -> unit

[<ServiceContract(Namespace = "https://www.tddstud10.r4nd0mapps.com", 
                  CallbackContract = typeof<ITestAdapterServiceCallback>)>]
type ITestAdapterService = 
    
    [<OperationContract>]
    abstract DiscoverTests : basePaths:(FilePath * FilePath)
     -> tdSearchPath:FilePath -> ignoredTestsPattern:string [] -> assemblyPath:FilePath -> unit
    
    [<OperationContract>]
    abstract ExecuteTests : teSearchPath:FilePath -> testCase:DTestCase2 -> DTestResult
    
    [<OperationContract>]
    abstract ExecuteTestsAndCollectCoverageData : teSearchPath:FilePath
     -> testCase:DTestCase2 -> PerDocumentSequencePoints2
