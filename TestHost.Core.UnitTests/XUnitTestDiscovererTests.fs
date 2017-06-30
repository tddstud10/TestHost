module R4nd0mApps.TddStud10.TestExecution.Adapters.XUnitTestDiscovererTests

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.XTestPlatform.Api
open System.Collections.Concurrent
open System.IO
open NUnit.Framework
open FsUnit

let getLocalPath = Path.getExecutingAssemblyPath >> FilePath
let expectedTests = 
    [ "XUnit20FSPortable.UnitTests.Fact Test 1"; 
      "XUnit20FSPortable.UnitTests.Fact Test 2"; 
      "XUnit20FSPortable.UnitTests.Theory Tests(input: 1)"; 
      "XUnit20FSPortable.UnitTests.Theory Tests(input: 2)" ]

let testBin = 
    ()
    |> getLocalPath
    |> fun (FilePath p) -> Path.Combine(p, "TestData/bins/XUnit20FSPortable/XUnit20FSPortable.dll")
    |> FilePath

let createDiscoverer() = 
    let td = new XUnitTestDiscoverer()
    let tcs = new ConcurrentBag<_>()
    td.TestDiscovered |> Observable.add tcs.Add
    td, tcs

[<Test>]
let ``Can run successfully on assemblies with no tests``() = 
    let it, _ = createDiscoverer()
    it.DiscoverTests([], testBin, Array.empty<string>)

[<Test>]
let ``Can discover theory and facts from test assembly``() = 
    let it, tcs = createDiscoverer()
    let td = getLocalPath().ToString() |> AdapterLoader.LoadDiscoverersFromPath [ "Xtensions/XUnit/R4nd0mApps.XTestPlatform.XUnit.dll" ]
    it.DiscoverTests(td, testBin, Array.empty<string>)
    let actualTests = 
        tcs
        |> Seq.map (fun t -> t.DisplayName)
        |> Seq.sort
        |> Seq.toList
    expectedTests |> should equal actualTests

[<Test>]
let ``Can ignore discover theory and facts from test assembly``() = 
    let it, tcs = createDiscoverer()
    let td = getLocalPath().ToString() |> AdapterLoader.LoadDiscoverersFromPath [ "Xtensions/XUnit/R4nd0mApps.XTestPlatform.XUnit.dll" ]
    let filteredTestName = "XUnit20FSPortable.UnitTests.Theory Tests"
    it.DiscoverTests(td, testBin, [| filteredTestName |])
    let filteredTests = expectedTests |> List.filter (fun f -> not (f.StartsWith(filteredTestName)))
    
    let actualTests = 
        tcs
        |> Seq.map (fun t -> t.DisplayName)
        |> Seq.sort
        |> Seq.toList
    filteredTests |> should equal actualTests
