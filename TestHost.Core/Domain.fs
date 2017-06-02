namespace R4nd0mApps.TddStud10.TestHost

open R4nd0mApps.TddStud10.Common.Domain
open System

[<CLIMutable>]
type DTestCase2 = 
    { TestCase : string
      DtcId : Guid
      FullyQualifiedName : string
      DisplayName : string
      Source : FilePath
      CodeFilePath : FilePath
      LineNumber : DocumentCoordinate }

type TestAssemblyId = string

type MethodMdRid = string

type SequencePointNumber = string

type TestCoverageData = seq<TestAssemblyId * MethodMdRid * SequencePointNumber>

[<CLIMutable>]
type DTestResultWithCoverageData = 
    { Result : DTestResult
      CoverageData : TestCoverageData }
