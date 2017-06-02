module R4nd0mApps.TddStud10.TestHost.TestAdapterExtensionsTests

open R4nd0mApps.TddStud10.Common
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.TddStud10.TestExecution
open global.Xunit
open FsUnit.Xunit
open Microsoft.VisualStudio.TestPlatform.ObjectModel

let testDataRoot = 
    FilePath.combine [ TestPlatformExtensions.getLocalPath()
                       FilePath "TestData" ]

let adapterMap = 
    [ (FilePath "aunit.testadapter.dll", 
       [ ("ITestDiscoverer", "AUnit.TestAdapter.VsDiscoverer")
         ("ITestExecutor", "AUnit.TestAdapter.VsTestExecutor") ]
       |> Map.ofList)
      (FilePath "bUnit.Testadapter.dll", 
       [ ("ITestDiscoverer", "BUnit.TestAdapter.VsTestDiscoverer")
         ("ITestExecutor", "BUnit.TestAdapter.VsTestExecutor") ]
       |> Map.ofList) ]
    |> Map.ofList

let ``Test Data for Nested search path with no adapters, return empty`` : obj array seq = 
    [| TestAdapterExtensions.findTestDiscoverers adapterMap >> Seq.map box
       TestAdapterExtensions.findTestExecutors adapterMap >> Seq.map box |]
    |> Seq.map (fun a -> [| box a |])

[<Theory>]
[<MemberData("Test Data for Nested search path with no adapters, return empty")>]
let ``Non existant path, return empty`` (f : FilePath -> obj seq) = 
    let sp = 
        FilePath.combine [ testDataRoot
                           FilePath "NonExistantPath" ]
    Assert.Empty(sp |> f)

[<Theory>]
[<MemberData("Test Data for Nested search path with no adapters, return empty")>]
let ``Nested search path with no adapters, return empty`` (f : FilePath -> obj seq) = 
    let sp = 
        FilePath.combine [ testDataRoot
                           FilePath "NoTestAdapters" ]
    Assert.NotEmpty(sp |> FilePath.enumerateFiles System.IO.SearchOption.AllDirectories "*.*")
    Assert.Empty(sp |> f)

let ``Test Data for Nested Search path with 2 adapters, return both`` : obj array seq = 
    [| (TestAdapterExtensions.findTestDiscoverers adapterMap >> Seq.map box, 
        [ "AUnit.TestAdapter.VsDiscoverer"; "BUnit.TestAdapter.VsTestDiscoverer" ])
       
       (TestAdapterExtensions.findTestExecutors adapterMap >> Seq.map box, 
        [ "AUnit.TestAdapter.VsTestExecutor"; "BUnit.TestAdapter.VsTestExecutor" ]) |]
    |> Seq.map (fun (a, b) -> 
           [| box a
              box b |])

[<Theory>]
[<MemberData("Test Data for Nested Search path with 2 adapters, return both")>]
let ``Nested Search path with 2 adapters, return both`` (f : FilePath -> obj seq, adapters : string list) = 
    let sp = 
        FilePath.combine [ testDataRoot
                           FilePath "TestAdapters" ]
    let found = 
        sp
        |> f
        |> Seq.map (fun d -> d.GetType().FullName)
        |> Seq.sort
        |> List.ofSeq
    
    Assert.Equal<string seq>(found, adapters)

let testBin = 
    [ TestPlatformExtensions.getLocalPath(); (FilePath @"TestData\bins\XUnit20FSPortable\XUnit20FSPortable.dll") ]
    |> FilePath.combine 

let expectedTests = 
    [ "XUnit20FSPortable.UnitTests.Fact Test 1", TOPassed
      "XUnit20FSPortable.UnitTests.Fact Test 2", TOFailed
      "XUnit20FSPortable.UnitTests.Theory Tests(input: 1)", TOPassed
      "XUnit20FSPortable.UnitTests.Theory Tests(input: 2)", TOFailed ]

let rebasePaths = (FilePath @"D:\XXX\UnitTestProjects\0\XUnit20FSPortable.fsproj", FilePath @"D:\src\t\testprojects\testexecution\testdata\unittestprojects\XUnit20FSPortable\XUnit20FSPortable.fsproj")

[<Fact>]
let ``discoverTests can ignore and discover theory and facts from test assembly``() = 
    let filteredTestName = "XUnit20FSPortable.UnitTests.Theory Tests"
   
    let discTests = 
        testBin
        |> TestAdapterExtensions.discoverTests rebasePaths (TestPlatformExtensions.getLocalPath()) [| filteredTestName |]
    
    let expected = 
        expectedTests
        |> List.map fst
        |> List.filter (String.startsWith filteredTestName >> not)
        |> List.map (Prelude.tuple2 @"D:\XXX\UNITTESTPROJECTS\0\PORTABLELIBRARY1.FS")
    let actual = 
        discTests
        |> Seq.map (fun t -> 
                    let t = DataContract.deserialize<TestCase>(t.TestCase)
                    t.CodeFilePath, t.DisplayName)
        |> Seq.sort
        |> Seq.toList
    actual |> should equal expected

[<Fact>]
let ``executeTest can execute tests``() = 
    let actual =
        testBin
        |> TestAdapterExtensions.discoverTests rebasePaths (TestPlatformExtensions.getLocalPath()) [| |]
        |> Seq.map (TestAdapterExtensions.executeTest (TestPlatformExtensions.getLocalPath()))
        |> Seq.map (fun tr -> tr.DisplayName, tr.Outcome)
        |> Seq.sortBy fst
        |> Seq.toList

    actual |> should equal expectedTests
