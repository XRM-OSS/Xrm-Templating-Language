// include Fake libs
#I @"packages\FAKE\tools\"
#r @"packages\FAKE\tools\FakeLib.dll"

open Fake
open Fake.Testing.NUnit3
open System
open System.IO
open System.Text.RegularExpressions
open Fake.Paket
open Fake.Git
open Fake.OpenCoverHelper
open Fake.ReportGeneratorHelper
open Fake.FileHelper

//Project config
let projectName = "Xrm.Oss.XTL"
let projectDescription = "A domain specific language for templating inside Dynamics CRM"
let authors = ["Florian Kroenert"]

// Directories
let buildDir  = @".\build\"
let pluginBuildDir = buildDir + @"plugin\"
let testDir   = @".\test\"

let deployDir = @".\Publish\"
let pluginDeployDir = deployDir + @"plugin"

let nugetDir = @".\nuget\"
let packagesDir = @".\packages\"

let nUnitPath = "packages" @@ "nunit.consolerunner" @@ "tools" @@ "nunit3-console.exe"
let sha = Git.Information.getCurrentHash()

// version info
let major = "3"
let minor = "9"
let patch = "9"

// Follow SemVer scheme: http://semver.org/
let asmVersion = major + "." + minor + "." + patch
let asmFileVersion  = asmVersion + "+" + sha

let pluginVersion = sprintf "1.0.%s" buildVersion

// Targets
Target "Clean" (fun _ ->

    CleanDirs [buildDir; testDir; deployDir; nugetDir]
)

Target "AssemblyInfo" (fun _ ->
    BulkReplaceAssemblyInfoVersions "src" (fun f -> 
                                              {f with
                                                  AssemblyVersion = pluginVersion
                                                  AssemblyInformationalVersion = pluginVersion
                                                  AssemblyFileVersion = pluginVersion
                                              })
)

Target "BuildPlugin" (fun _ ->
    !! @"src\plugin\Xrm.Oss.XTL.Templating\*.csproj"
        |> MSBuildRelease pluginBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildTest" (fun _ ->
    !! @"src\test\**\*.csproj"
      |> MSBuildDebug testDir "Build"
      |> Log "Build Log: "
)

Target "NUnit" (fun _ ->
    let testFiles = !!(testDir @@ @"\**\*.Tests.dll")
    
    if testFiles.Includes.Length <> 0 then
      testFiles
        |> NUnit3 (fun test ->
             {test with
                   ShadowCopy = false;
                   ToolPath = nUnitPath;})
)

Target "Publish" (fun _ ->
    CreateDir pluginDeployDir
    
    !! (pluginBuildDir @@ @"*.*")
        |> CopyTo pluginDeployDir
)

Target "CodeCoverage" (fun _ ->
    OpenCover (fun p -> { p with 
                                TestRunnerExePath = nUnitPath
                                ExePath ="packages" @@ "OpenCover" @@ "tools" @@ "OpenCover.Console.exe"
                                Register = RegisterType.RegisterUser
                                WorkingDir = (testDir)
                                Filter = "+[Xrm.Oss*]Xrm.Oss.XTL.* -[*.Tests*]*"
                                Output = "../coverage.xml"
                        }) "Xrm.Oss.XTL.Interpreter.Tests.dll Xrm.Oss.XTL.Templating.Tests.dll"
)

Target "ReportCodeCoverage" (fun _ ->
    ReportGenerator (fun p -> { p with 
                                    ExePath = "packages" @@ "ReportGenerator" @@ "tools" @@ "net47" @@ "ReportGenerator.exe"
                                    WorkingDir = (testDir)
                                    TargetDir = "../reports"
                                    ReportTypes = [ReportGeneratorReportType.Html; ReportGeneratorReportType.Badges ]
                               }) [ "..\coverage.xml" ]
    
)

Target "CreateNuget" (fun _ ->
    Pack (fun p ->
            {p with
                Version = asmVersion
            })
)

// Dependencies
"Clean"
  =?> ("AssemblyInfo", not isLocalBuild )
  ==> "BuildPlugin"
  ==> "BuildTest"
  ==> "NUnit"
  ==> "CodeCoverage"
  ==> "ReportCodeCoverage"
  ==> "Publish"
  ==> "CreateNuget"

// start build
RunTargetOrDefault "Publish"
