namespace R4nd0mApps.TddStud10.TestExecution.Adapters

open Microsoft.FSharp.Control
open R4nd0mApps.TddStud10.Common.Domain
open R4nd0mApps.XTestPlatform.Api

type XUnitTestDiscoverer() = 
    let filteredTest = new Event<_>()
    let testDiscovered = new Event<_>()
    
    let isValidTest ignoredTests (testCase : XTestCase) = 
        ignoredTests
        |> Array.exists testCase.FullyQualifiedName.StartsWith
        |> not
    
    member public __.TestDiscovered = filteredTest.Publish
    member public __.DiscoverTests(tds : IXTestDiscoverer seq, FilePath asm, ignoredTests : string []) = 
        testDiscovered.Publish
        |> Event.filter (fun testCase -> isValidTest ignoredTests testCase)
        |> Event.add (fun testCase -> filteredTest.Trigger(testCase))
        tds
        |> Seq.map (fun td -> 
                    use __ = td.TestDiscovered.Subscribe(testDiscovered.Trigger)
                    td.DiscoverTests([ asm ]))
        |> Seq.fold (fun _ -> id) ()
