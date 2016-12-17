// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing
open System
open System.IO

MSBuildDefaults <- { MSBuildDefaults with Verbosity = Some MSBuildVerbosity.Minimal }

// Directories
let packagesDir = __SOURCE_DIRECTORY__ @@ "packages"
let buildDir  = __SOURCE_DIRECTORY__ @@ @"build"
let testDir  = __SOURCE_DIRECTORY__ @@ @"build"
let nugetDir = __SOURCE_DIRECTORY__ @@ @"NuGet"
ensureDirExists (directoryInfo nugetDir)

// Filesets
let solutionFile = "TestHost.sln"

let msbuildProps = [
    "Configuration", "Debug"
    "Platform", "Any CPU"
]

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir]

    !! solutionFile
    |> MSBuild buildDir "Clean" msbuildProps
    |> ignore
)

Target "Rebuild" DoNothing

Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuild buildDir "Build" msbuildProps
    |> ignore
)

Target "GitLink" (fun _ ->
    let gitLink = (packagesDir @@ @"gitlink" @@ "lib" @@ "net45" @@ "GitLink.exe")
    let args = sprintf "%s -f %s -d %s" __SOURCE_DIRECTORY__ solutionFile buildDir
    let ret =
        ExecProcessAndReturnMessages (fun info ->
            info.FileName <- gitLink
            info.Arguments <- args) (TimeSpan.FromSeconds 30.0)
    let consoleOutput =
        ret.Messages
        |> Seq.append ret.Errors
    consoleOutput
    |> Seq.iter (printfn "%s")
    let loadFailures =
        consoleOutput
        |> Seq.filter (fun m -> m.ToLowerInvariant().Contains("failed to load project"))
    if not ret.OK || not (Seq.isEmpty loadFailures) then failwith (sprintf "GitLink.exe \"%s\" task failed.\nErrors:\m %A" args loadFailures)
)

let runTest pattern =
    fun _ ->
        !! (buildDir + pattern)
        |> xUnit (fun p ->
            { p with
                ToolPath = findToolInSubPath "xunit.console.exe" (currentDirectory @@ "tools" @@ "xUnit")
                WorkingDir = Some testDir })

Target "Test" DoNothing
Target "UnitTests" (runTest "/*.UnitTests*.dll")

Target "Package" (fun _ ->
    "TestHost.nuspec"
    |> NuGet (fun p -> 
        { p with               
            Authors = [ "The TddStud10 Team" ]
            Project = "TddStud10.TestHost"
            Description = "TddStud10 TestHost"
            Version = EnvironmentHelper.environVarOrDefault "GitVersion_NuGetVersion" "0.0.0"
            Dependencies = [ "FSharp.Core", GetPackageVersion packagesDir "FSharp.Core" 
                             "TddStud10.Common", "0.0.0" ]
            Files = [ "R4nd0mApps.TddStud10.TestHost.Core.dll"
                      "R4nd0mApps.TddStud10.TestHost.Core.pdb"
                      "R4nd0mApps.TddStud10.TestRuntime.dll"
                      "R4nd0mApps.TddStud10.TestRuntime.pdb"
                      "TddStud10.TestHost.exe"
                      "TddStud10.TestHost.pdb"
                      "TddStud10.TestHost.exe.config" ] 
                    |> List.map (fun f -> buildDir @@ f, Some "lib", None )
            OutputPath = buildDir })
)

Target "Publish" (fun _ ->
    !! "build/*.nupkg"
    |> AppVeyor.PushArtifacts
)

"Clean" ?=> "Build"
"Clean" ==> "Rebuild" 
"Build" ==> "Rebuild" 
"Build" ?=> "UnitTests" ==> "Test"
"Rebuild" ==> "Test"
"GitLink" ==> "Package"
"Test" ?=> "GitLink"
"Test" ==> "Package" ==> "Publish"

// start build
RunTargetOrDefault "Test"
