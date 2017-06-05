module R4nd0mApps.TddStud10.TestHost.TestAdapterExtensionsTests

open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.TestExecution
open global.Xunit
open FsUnit.Xunit
open R4nd0mApps.TddStud10.TestHost.TestAdapterExtensions
open R4nd0mApps.XTestPlatform.Api

let adapterSearchPath = Path.getLocalPath() |> FilePath

let testDataRoot = 
    FilePath.combine [ adapterSearchPath
                       FilePath "TestData" ]


let testBin = 
    [ adapterSearchPath; (FilePath @"TestData/bins/XUnit20FSPortable/XUnit20FSPortable.dll") ]
    |> FilePath.combine 

let expectedTests = 
    [ "XUnit20FSPortable.UnitTests.Fact Test 1", XTestOutcome.Passed
      "XUnit20FSPortable.UnitTests.Fact Test 2", XTestOutcome.Failed
      "XUnit20FSPortable.UnitTests.Theory Tests(input: 1)", XTestOutcome.Passed
      "XUnit20FSPortable.UnitTests.Theory Tests(input: 2)", XTestOutcome.Failed ]

let rebasePaths = (FilePath @"D:\XXX\UnitTestProjects\0\XUnit20FSPortable.fsproj", FilePath @"D:\src\t\testprojects\testexecution\testdata\unittestprojects\XUnit20FSPortable\XUnit20FSPortable.fsproj")

[<Fact>]
let ``discoverTests can ignore and discover theory and facts from test assembly``() = 
    let filteredTestName = "XUnit20FSPortable.UnitTests.Theory Tests"
   
    let discTests = 
        testBin
        |> TestAdapterExtensions.discoverTests rebasePaths adapterSearchPath [| filteredTestName |]
    
    let expected = 
        expectedTests
        |> List.map fst
        |> List.filter (String.startsWith filteredTestName >> not)
        |> List.map (Prelude.tuple2 @"D:\XXX\UNITTESTPROJECTS\0\PORTABLELIBRARY1.FS")
    let actual = 
        discTests
        |> Seq.map (fun t -> t.CodeFilePath, t.DisplayName)
        |> Seq.sort
        |> Seq.toList
    actual |> should equal expected

[<Fact>]
let ``executeTest can execute tests``() = 
    let actual = 
        testBin
        |> TestAdapterExtensions.discoverTests rebasePaths adapterSearchPath [||]
        |> Seq.map (TestAdapterExtensions.executeTest adapterSearchPath >> (fun tr -> tr.DisplayName, tr.Outcome))
        |> Seq.sortBy fst
        |> Seq.toList
    actual |> should equal expectedTests

