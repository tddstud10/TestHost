// include Fake libs
#r "./packages/Build/FAKE/tools/FakeLib.dll"
#if MONO
#else
#r "System.Management.Automation"
#endif

open Fake
open Fake.Testing
open System
open System.IO
#if MONO
#else
open System.Management.Automation
#endif

MSBuildDefaults <- { MSBuildDefaults with Verbosity = Some MSBuildVerbosity.Minimal }
setEnvironVar "MONO_ENV_OPTIONS" "--debug"
let assemblyVersion = EnvironmentHelper.environVarOrDefault "GitVersion_AssemblySemVer" "0.1.0.0"

// Directories
let packagesDir = __SOURCE_DIRECTORY__ @@ "packages" @@ "Build"
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

#if MONO
#else
Target "UpdateAppConfig" (fun _ ->
    PowerShell
        .Create()
        .AddScript(__SOURCE_DIRECTORY__ @@ "tools" @@ (sprintf "UpdateAppConfig.ps1 -BuildDir %s -Version %s" buildDir assemblyVersion))
        .Invoke()
        |> Seq.iter (printfn "%O")
)
#endif

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

let runTestXUnit pattern =
    fun _ ->
        !! (buildDir @@  pattern)
        |> xUnit (fun p ->
            { p with
                ToolPath = findToolInSubPath "xunit.console.exe" ""
                WorkingDir = Some testDir })

let runTestNUnit pattern =
    fun _ ->
        !! (buildDir @@  pattern)
        |> NUnit3 (fun p ->
            { p with
                ToolPath = findToolInSubPath "nunit3-console.exe" @"C:\Tools\NUnit3"
                WorkingDir = testDir })

Target "Test" DoNothing
Target "UnitTests" (runTestXUnit "*.UnitTests*.dll" >> runTestNUnit "*.UnitTests*.dll")
Target "ContractTests" (runTestXUnit "*.ContractTests*.dll") 

Target "Package" (fun _ ->
    "TestHost.nuspec"
    |> NuGet (fun p -> 
        { p with               
            Authors = [ "The TddStud10 Team" ]
            Project = "TddStud10.TestHost"
            Description = "TddStud10 TestHost"
            Version = EnvironmentHelper.environVarOrDefault "GitVersion_NuGetVersion" "0.0.0-alpha00"
            Dependencies = [ "FSharp.Core", ".." 
                             "Mono.Cecil", ".."
                             "R4nd0mApps.XTestPlatform", "../TddStud10"
                             "TddStud10.Common", "../TddStud10" ]
                           |> List.map (fun (d,g) -> d, GetPackageVersion (packagesDir @@ g) d)
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
#if MONO
#else
"UpdateAppConfig" ==> "ContractTests"
#endif
"Build" ?=> "ContractTests" ==> "Test"
"Rebuild" ==> "Test"
"GitLink" ==> "Package"
"Test" ?=> "GitLink"
"Test" ==> "Package" ==> "Publish"

// start build
RunTargetOrDefault "Test"
