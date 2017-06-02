namespace R4nd0mApps.TddStud10.TestHost

open System
open R4nd0mApps.TddStud10.TestRuntime
open System.ServiceModel
open R4nd0mApps.TddStud10.Common.Domain
open System.Collections.Concurrent

[<ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)>]
type CoverageDataCollector() = 
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger

    let coverageData = new PerSequencePointIdTestRunId()
    let tempStore = new ConcurrentDictionary<string, ConcurrentBag<string * string * string>>()
    
    let enterSequencePoint testRunId assemblyId methodMdRid spId = 
        if isNull testRunId || isNull assemblyId || isNull methodMdRid || isNull spId then 
            logger.logErrorf "CoverageDataCollector: EnterSequencePoint: Invalid payload: %s %s %s %s" testRunId 
                assemblyId methodMdRid spId
        else 
            let list = tempStore.GetOrAdd(testRunId, fun _ -> ConcurrentBag<_>())
            list.Add(assemblyId, methodMdRid, spId)
    
    let exitUnitTest testRunId source document line = 
        if isNull testRunId || isNull source || isNull document || isNull line then 
            logger.logErrorf "CoverageDataCollector: ExitUnitTest: Unexpected payload in ExitUnitTest: %s %s %s %s" 
                testRunId source document line
        else 
            let exists, sps = tempStore.TryRemove(testRunId)
            if not exists then 
                logger.logErrorf 
                    "CoverageDataCollector: ExitUnitTest: Did not have any sequence points in thread %s for %s,%s,%s." 
                    testRunId source document line
            else 
                let addSPC a m s = 
                    let spId = 
                        { methodId = 
                              { assemblyId = AssemblyId(Guid.Parse(a))
                                mdTokenRid = MdTokenRid(UInt32.Parse(m)) }
                          uid = Int32.Parse(s) }
                    
                    let l = coverageData.GetOrAdd(spId, fun _ -> ConcurrentBag<_>())
                    { testId = 
                          { source = source |> FilePath
                            location = { document = document |> FilePath
                                         line = DocumentCoordinate(Int32.Parse(line)) } }
                      testRunInstanceId = TestRunInstanceId(Int32.Parse(testRunId)) }
                    |> l.Add
                    |> ignore
                Async.Parallel [ for sp in sps -> async { return sp |||> addSPC } ]
                |> Async.RunSynchronously
                |> ignore
                logger.logInfof "CoverageDataCollector: Servicing ExitUnitTest: %s,%s,%s. Sequence Points = %d" source 
                    document line sps.Count
    
    member __.CoverageData = coverageData
    interface ICoverageDataCollector with
        member __.EnterSequencePoint(testRunId : string, assemblyId : string, methodMdRid : string, spId : string) : unit = 
            enterSequencePoint testRunId assemblyId methodMdRid spId
        member __.ExitUnitTest(testRunId : string, source : string, document : string, line : string) : unit = 
            exitUnitTest testRunId source document line
        member __.Ping() : unit = logger.logInfof "CoverageDataCollector - responding to ping."

[<ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)>]
type CoverageDataCollector2Service() = 
    let logger = R4nd0mApps.TddStud10.Logger.LoggerFactory.logger
    
    let coverageData = ref <| ConcurrentBag<string * string * string>()

    member __.GetCoverageData () =
        let ret = coverageData.Value
        coverageData.Value <- ConcurrentBag<string * string * string>()
        ret :> seq<_>

    interface ICoverageDataCollector2 with
        member __.EnterSequencePoint(assemblyId : string, methodMdRid : string, spNum : string) : unit = 
            if isNull assemblyId || isNull methodMdRid || isNull spNum then 
                logger.logErrorf "CoverageDataCollector2Service: EnterSequencePoint: Invalid payload: %s %s %s" 
                    assemblyId methodMdRid spNum
            else 
                coverageData.Value.Add(assemblyId, methodMdRid, spNum)
