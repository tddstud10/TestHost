module R4nd0mApps.TddStud10.TestExecution.Adapters.XUnitTestExecutorTests

open R4nd0mApps.TddStud10.TestExecution
open System.Collections.Concurrent
open System.IO
open System.Runtime.Serialization
open System.Xml
open Xunit
open R4nd0mApps.TddStud10.Common.Domain
open System.Resources
open System.Reflection
open R4nd0mApps.XTestPlatform.Api

let getLocalPath = Path.getLocalPath >> FilePath

let testBin = 
    ()
    |> getLocalPath
    |> fun (FilePath p) -> Path.Combine(p, "TestData/bins/XUnit20FSPortable/XUnit20FSPortable.dll")
    |> FilePath

let expectedTests = 
    [ "XUnit20FSPortable.UnitTests.Fact Test 1", XTestOutcome.Passed
      "XUnit20FSPortable.UnitTests.Fact Test 2", XTestOutcome.Failed
      "XUnit20FSPortable.UnitTests.Theory Tests(input: 1)", XTestOutcome.Passed
      "XUnit20FSPortable.UnitTests.Theory Tests(input: 2)", XTestOutcome.Failed ]

let createDiscoverer() = 
    let td = new XUnitTestDiscoverer()
    let tcs = new ConcurrentBag<_>()
    td.TestDiscovered |> Observable.add tcs.Add
    td, tcs

let createExecutor() = 
    let te = new XUnitTestExecutor()
    let trs = new ConcurrentQueue<_>()
    te.TestExecuted |> Observable.add trs.Enqueue
    te, trs

[<Fact>]
let ``Can run successfully on assemblies with no tests``() = 
    let it, _ = createExecutor()
    it.ExecuteTests([ ], [||])

[<Fact>]
let ``Can run re-hydrated tests``() = 
    let it, tests = createDiscoverer()
    let td = getLocalPath().ToString() |> AdapterLoader.LoadDiscoverers
    it.DiscoverTests(td, testBin, Array.empty<string>)
    
    let te = getLocalPath().ToString() |> AdapterLoader.LoadExecutors
    let it, tos = createExecutor()
    it.ExecuteTests(te, tests)
    let actualTests = 
        tos
        |> Seq.map (fun t -> t.DisplayName, t.Outcome)
        |> Seq.sortBy fst
        |> Seq.toList
    Assert.Equal<list<_>>(expectedTests, actualTests)
