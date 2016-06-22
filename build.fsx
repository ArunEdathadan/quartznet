#r "packages/FAKE/tools/FakeLib.dll"

open Fake
open System
open System.Collections.Generic
open System.IO

let configuration = getBuildParamOrDefault "configuration" "Debug"

// helpers

let inline FileName fullName = Path.GetFileName fullName

let UpdateVersion version project =
    log ("Updating version in " + project)   
    ReplaceInFile (fun s -> replace "1.0.0-ci" version s) project

let CopyArtifact artifact =
    log ("Copying artifact " + (FileName artifact))
    ensureDirectory "artifacts"
    CopyFile "artifacts" artifact

// targets

Target "Clean" (fun _ ->
    !! "artifacts" ++ "src/*/bin" ++ "test/*/bin" ++ "build" ++ "deploy"
        |> CleanDirs
)

Target "UpdateVersions" (fun _ ->    
    let version = if buildServer <> BuildServer.LocalBuild then buildVersion else "1.0.0"
    
    !! "src/*/project.json" ++ "test/*/project.json"
        |> Seq.iter(UpdateVersion version)
)

Target "Build" (fun _ ->
    !! "src/*/project.json" -- "src/*Web*/project.json"
        |> DotNetCli.Build
            (fun p -> 
                { p with 
                    Configuration = configuration })
)

Target "CopyArtifacts" (fun _ ->    
    !! "src/*/bin/**/*.nupkg" 
        |> Seq.iter(CopyArtifact)
)

Target "Test" (fun _ ->
    !! "src/Quartz.Tests.Unit/project.json"
        |>  DotNetCli.Test
            (fun p -> 
                    { p with 
                        Configuration = configuration
                        AdditionalArgs = ["--where \"cat != \"database\""] })
)

"Clean"
  //==> "UpdateVersions"
  ==> "CopyArtifacts"
  ==> "Test"
  ==> "Build"

"Clean"
  //==> "UpdateVersions"
  ==> "Build"

"Clean"
  //==> "UpdateVersions"
  ==> "Test"

RunTargetOrDefault "Build"
