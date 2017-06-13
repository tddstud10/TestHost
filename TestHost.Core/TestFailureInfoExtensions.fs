namespace R4nd0mApps.TddStud10.TestHost

open R4nd0mApps.TddStud10.Common.Domain

module TestFailureInfoExtensions = 
    open R4nd0mApps.XTestPlatform.Api
    
    let create (tr : XTestResult) : seq<DocumentLocation * TestFailureInfo> = 
        let innerFn fi = 
            let tfi = 
                { message = 
                      if fi.Message = null then ""
                      else fi.Message
                  stack = 
                      fi.CallStack |> Array.map (function 
                                          | XUnparsedFrame x -> UnparsedFrame x
                                          | XParsedFrame(m, f, l) -> 
                                              ParsedFrame(m, 
                                                          { document = f |> FilePath
                                                            line = l |> DocumentCoordinate })) }
            tfi.stack |> Seq.choose (function 
                             | ParsedFrame(_, dl) -> Some(dl, tfi)
                             | _ -> Option.None)
        tr.FailureInfo |> Option.fold (Prelude.ct innerFn) Seq.empty
