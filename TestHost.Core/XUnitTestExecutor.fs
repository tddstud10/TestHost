namespace R4nd0mApps.TddStud10.TestExecution.Adapters

open R4nd0mApps.XTestPlatform.Api

type XUnitTestExecutor() = 
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

    let testExecuted = new Event<_>()
    
    let createExeSink cb = 
        { new IXTestCaseExecutionSink with
              member __.RecordResult(testResult : XTestResult) : unit = testResult |> cb
              member __.SendMessage(ml : XTestMessageLevel, m : string) : unit = logger.logInfof "Message from test case: %A: %s" ml m }
    
    member public __.TestExecuted = testExecuted.Publish
    member public __.ExecuteTests(tes : IXTestExecutor seq, tests : XTestCase seq) = 
        tes
        |> Seq.map (fun te -> 
               let ts = tests |> Seq.filter (fun t -> t.ExtensionUri = te.ExtensionUri)
               te.RunTests(ts, createExeSink testExecuted.Trigger))
        |> Seq.fold (fun _ -> id) ()
