namespace R4nd0mApps.TddStud10.TestHost

open R4nd0mApps.XTestPlatform.Api

type TestAssemblyId = string

type MethodMdRid = string

type SequencePointNumber = string

type TestCoverageData = seq<TestAssemblyId * MethodMdRid * SequencePointNumber>

[<CLIMutable>]
type DTestResultWithCoverageData = 
    { Result : XTestResult
      CoverageData : TestCoverageData }
