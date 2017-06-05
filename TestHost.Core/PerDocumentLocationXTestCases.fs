namespace R4nd0mApps.TddStud10.TestHost

open R4nd0mApps.TddStud10.Common.Domain
open System
open System.Collections.Concurrent
open R4nd0mApps.XTestPlatform.Api

[<Serializable>]
type PerDocumentLocationXTestCases = 
    inherit DataStoreEntityBase<DocumentLocation, ConcurrentBag<XTestCase>>
    
    new() = 
        { inherit DataStoreEntityBase<_, _>() }
        then ()
    
    member public t.Serialize path = DataStoreEntityExtensions.Serialize<PerDocumentLocationXTestCases> path t
    static member public Deserialize path = DataStoreEntityExtensions.Deserialize<PerDocumentLocationXTestCases>(path)
