namespace R4nd0mApps.TddStud10.TestHost

open R4nd0mApps.TddStud10.Common.Domain
open System
open System.Collections.Generic

[<CLIMutable>]
type DTestCase2 = 
    { TestCase : string
      DtcId : Guid
      FullyQualifiedName : string
      DisplayName : string
      Source : FilePath
      CodeFilePath : FilePath
      LineNumber : DocumentCoordinate }

type PerDocumentSequencePoints2 = IReadOnlyDictionary<FilePath, seq<SequencePoint>>
