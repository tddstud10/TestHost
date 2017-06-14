module R4nd0mApps.TddStud10.TestHost.ContractTests

open FsUnit.Xunit
open R4nd0mApps.TddStud10.Common.Domain
open System.Collections.Concurrent
open System.IO
open global.Xunit
open R4nd0mApps.XTestPlatform.Api

let adapterSearchPath = Path.getLocalPath() |> FilePath

let assemblyPath = 
    (Path.getLocalPath(), @"TestData2/CSXUnit1xNUnit3x.dll")
    |> Path.Combine
    |> FilePath

let rebasePaths = 
    FilePath @"D:/src/t/Engine/TestProjects/CSXUnit1xNUnit3x.NET20/Sln.sln", 
    FilePath @"D:/delme/_tdd/CSXUnit1xNUnit3x.NET20/Sln.sln"

[<Fact>]
let ``Adapter service can discover and run tests``() = 
    let testCases = ConcurrentBag<XTestCase>()
    let svcCB = TestAdapterServiceFactory.TestAdapterServiceCallback()
    svcCB.Callback <- testCases.Add
    let proc, svc = TestAdapterServiceFactory.create svcCB
    use proc = proc
    svc.DiscoverTests rebasePaths adapterSearchPath [||] assemblyPath
    let results = 
        testCases
        |> Seq.map (fun tc -> 
               let x = tc |> DataContract.serialize |> svc.ExecuteTest adapterSearchPath
               x.DisplayName, x.Outcome)
        |> Seq.sortBy fst
        |> Seq.toArray
    results |> should equal [| """CSXUnit1xNUnit3x.StringTests3.IndexOf(input: "hello world", letter: 'w', expected: 6)""", XTestOutcome.Passed
                               """CSXUnit1xNUnit3x.StringTests3.TestToSkip""", XTestOutcome.Passed |]

[<Fact>]
let ``Adapter service can run tests and collect coverage data``() = 
    let testCases = ConcurrentBag<XTestCase>()
    let svcCB = TestAdapterServiceFactory.TestAdapterServiceCallback()
    svcCB.Callback <- testCases.Add
    let proc, svc = TestAdapterServiceFactory.create svcCB
    use proc = proc
    svc.DiscoverTests rebasePaths adapterSearchPath [||] assemblyPath
    let results = 
        testCases
        |> Seq.map (DataContract.serialize >> svc.ExecuteTestsAndCollectCoverageData adapterSearchPath)
        |> Seq.toArray
        |> Array.map (fun r -> 
               let x = r.Result
               (x.DisplayName, x.Outcome), r.CoverageData)
        |> Array.sortBy fst
    results
    |> Array.map fst
    |> should equal [| """CSXUnit1xNUnit3x.StringTests3.IndexOf(input: "hello world", letter: 'w', expected: 6)""", XTestOutcome.Passed
                       """CSXUnit1xNUnit3x.StringTests3.TestToSkip""", XTestOutcome.Passed |]
    results
    |> Array.map snd
    |> should equal [| [| "fff51eec-939d-4472-9c08-b9a275f81d6d", "1", "3"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "1", "2"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "1", "1"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "1", "0"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "4", "1"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "4", "0"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "1", "3"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "1", "2"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "1", "1"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "1", "0"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "4", "1"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "4", "0"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "5", "1"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "5", "0"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "7", "0" |]
                       [| "fff51eec-939d-4472-9c08-b9a275f81d6d", "3", "2"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "3", "1"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "3", "0"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "4", "1"
                          "fff51eec-939d-4472-9c08-b9a275f81d6d", "4", "0" |] |]
