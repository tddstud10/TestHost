namespace R4nd0mApps.TddStud10.TestExecution.Adapters

open R4nd0mApps.XTestPlatform.Api

type XUnitTestExecutor() = 
    let testExecuted = new Event<_>()
    
    member public __.TestExecuted = testExecuted.Publish
    member public __.ExecuteTests(tes : IXTestExecutor seq, tests : XTestCase seq) = 
        tes
        |> Seq.map (fun te -> 
               let ts = tests |> Seq.filter (fun t -> t.ExtensionUri = te.ExtensionUri)
               use __ = te.TestExecuted.Subscribe(testExecuted.Trigger)
               te.RunTests(ts))
        |> Seq.fold (fun _ -> id) ()
