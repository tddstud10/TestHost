namespace R4nd0mApps.TddStud10.TestHost

open R4nd0mApps.TddStud10.Common.Domain

module TestFailureInfoExtensions = 
    open R4nd0mApps.XTestPlatform.Api
    
    let create (tr : XTestResult) : seq<DocumentLocation * TestFailureInfo> = 
        let innerFn fi = 
            let tfi = 
                { message = 
                      let (XErrorMessage m) = fi.Message
                      m
                  stack = 
                      let (XErrorStackTrace cs) = fi.CallStack
                      cs |> Array.map (function 
                                          | XErrorUnparsedFrame x -> UnparsedFrame x
                                          | XErrorParsedFrame(m, f, l) -> 
                                              ParsedFrame(m, 
                                                          { document = f |> FilePath
                                                            line = l |> DocumentCoordinate })) }
            tfi.stack |> Seq.choose (function 
                             | ParsedFrame(_, dl) -> Some(dl, tfi)
                             | _ -> Option.None)
        tr.FailureInfo |> Option.fold (Prelude.ct innerFn) Seq.empty
