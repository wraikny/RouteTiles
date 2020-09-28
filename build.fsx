#r "paket:
source https://api.nuget.org/v3/index.json
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target
nuget Fake.Core.ReleaseNotes
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Testing.Expecto
nuget FAKE.IO.Zip
nuget FAKE.Net.Http
nuget FSharp.Json //"

#load ".fake/build.fsx/intellisense.fsx"
#r "netstandard"

open System
open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Net
open FSharp.Json

let altseed2CommitId = "95f965be486427a94b8ebc1c5e676c447cd5923d"
let runtimes = [ "linux-x64"; "win-x64"; "osx-x64" ]
let resourcesDirectory = @"Resources"
let altseed2Dir = @"lib/Altseed2"

let testProjects = []

let publishOutput = sprintf "publish/RouteTiles.%s"

let resourcesf fmt = Printf.kprintf (sprintf "%s%s" resourcesDirectory) fmt

let dotnet cmd arg =
  let res = DotNet.exec id cmd arg
  if not res.OK then
    failwithf "Failed 'dotnet %s %s'" cmd arg

Target.initEnvironment()

Target.create "Test" (fun _ ->
  [ for x in testProjects ->
      sprintf "tests/%s/bin/Release/**/%s.dll" x x
  ] |> function
  | [] ->
    printfn "There is no test project"
  | x::xs ->
    Seq.fold (++) (!! x) xs
    |> Expecto.run id
)

Target.create "Tool" (fun _ ->
  dotnet "tool" "update fake-cli"
)

Target.create "Clean" (fun _ ->
  !! "src/**/bin"
  ++ "src/**/obj"
  ++ "tests/**/bin"
  ++ "tests/**/obj"
  ++ "lib/**/bin"
  ++ "lib/**/obj"
  ++ "publish"
  |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
  !! "src/**/*.*proj"
  ++ "tests/**/*.*proj"
  |> Seq.iter (DotNet.build id)
)


Target.create "Serve" (fun _ ->
  Shell.cd @"SRServer"
  dotnet "run" "--project ../lib/simple-rankings-server/src/SimpleRankingsServer"
  ()
)


Target.create "Resources" (fun _ ->
  File.delete "Resources.pack"
  File.delete "Resources.zip"
  
  Shell.cleanDir (resourcesf "/Shader")

  Shell.copyDir (resourcesf "/Shader") @"src/Shader" (fun _ -> true)

  dotnet "fsi" "--exec pack.fsx"

  let targetProject = "RouteTiles"

  let outDir x = sprintf "src/%s/bin/%s/netcoreapp3.1" targetProject x

  // for Debug
  (
    let dir = outDir "Debug"
    let target = sprintf "%s/%s" dir resourcesDirectory
    Directory.ensure dir
    Directory.delete target |> ignore
    Shell.copyDir target resourcesDirectory (fun _ -> true)
    Trace.tracefn "Finished copying %s to %s" resourcesDirectory target
  )

  // for Release
  (
    let packedResources = resourcesf ".pack"
    let target = outDir "Release"
    Directory.ensure target
    Shell.copyFile target packedResources
    Trace.tracefn "Finished copying %s to %s" packedResources target
  )

  // for Backup
  !!(resourcesf "/**")
  |> Zip.zip resourcesDirectory (resourcesf ".zip")

  !!("Fonts/**")
  |> Zip.zip "Fonts" ("Fonts.zip")
)


Target.create "Publish" (fun _ ->
  Trace.tracefn "Clean 'publish'"
  Shell.cleanDir "publish"

  runtimes
  |> Seq.iter (fun target ->
    Trace.tracefn "Start for '%s'" target

    let outputPath = publishOutput target

    Directory.ensure outputPath

    // Copy Texts
    "publishContents/README.md"
    |> Shell.copyFile (sprintf "%s/README.txt" outputPath)

    // LICENSES
    [|
      "RouteTiles", "LICENSE"
      ".NET Core", "publishContents/LICENSES/dotnetcore.txt"
      "FSharp.Json", "publishContents/LICENSES/fsharp.json.txt"
      "SimpleRankingServer", "publishContents/LICENSES/simplerankingserver.txt"
      "Affogato", "lib/Affogato/LICENSE"
      "EffFs", "lib/EffFs/LICENSE"
      "Altseed2", "lib/Altseed2/LICENSE"
      "Altseed2.BoxUI", "lib/Altseed2.BoxUI/LICENSE"
    |]
    |> Array.map (fun (libname, path) -> sprintf "%s\n\n%s" libname (File.readAsString path))
    |> String.concat (sprintf "\n%s\n" <| String.replicate 50 "-")
    |> File.writeString false (sprintf "%s/LICENSE.txt" outputPath)

    // Copy Resources
    let packedResources = resourcesf ".pack"
    Trace.tracefn "Copy %s" packedResources
    packedResources
    |> Shell.copyFile (sprintf "%s/%s" outputPath packedResources)

    let isTargetOSX = target.Contains("osx")
    let isTargetLinux = target.Contains("linux")

    let targetShellFileName = 
      if isTargetOSX then Some "RouteTiles.command"
      elif isTargetLinux then Some "RouteTiles.sh"
      else None

    targetShellFileName
    |> Option.iter(fun shellfile ->
      "publishContents/RouteTiles.command"
      |> Shell.copyFile (sprintf "%s/%s" outputPath shellfile)
    )

    let binaryOutputDirectory =
      if isTargetOSX || isTargetLinux then sprintf "%s/Bin" outputPath else outputPath

    // Publish
    "src/RouteTiles/RouteTiles.fsproj"
    |> DotNet.publish (fun p ->
      { p with
          Runtime = Some target
          Configuration = DotNet.BuildConfiguration.Release
          SelfContained = Some true
          MSBuildParams = {
            p.MSBuildParams with
              Properties =
                ("PublishSingleFile", "true")
                :: ("PublishTrimmed", "true")
                :: p.MSBuildParams.Properties
          }
          OutputPath = binaryOutputDirectory |> Some
      }
    )

    let patform = Environment.OSVersion.Platform

    let isUnixLike = (patform = PlatformID.Unix || patform = PlatformID.MacOSX)
    
    if isUnixLike then
      targetShellFileName
      |> Option.iter(fun shellfile ->
        Shell.Exec("chmod", sprintf "+x %s/%s" outputPath shellfile) |> ignore
      )

      if isTargetOSX || isTargetLinux then
        Shell.Exec("chmod", sprintf "+x %s/RouteTiles" binaryOutputDirectory) |> ignore

    // Make zip
    Trace.tracefn "Make %s.zip" outputPath
    !! (sprintf "%s/**" outputPath)
    |> Zip.zip "publish" (sprintf "%s.zip" outputPath)
  )
)

Target.create "CopyLib" (fun _ ->
  [|
    @"lib/Altseed2.BoxUI/lib/Altseed2"
  |]
  |> Seq.iter (fun dir ->
    Shell.copyDir dir altseed2Dir (fun _ -> true)
  )
)

Target.create "Download" (fun _ ->
  let token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
  let url = @"https://api.github.com/repos/altseed/altseed2-csharp/actions/artifacts"

  use client = new Net.Http.HttpClient()
  client.DefaultRequestHeaders.UserAgent.ParseAdd("wraikny.RouteTiles")
  client.DefaultRequestHeaders.Authorization <- Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)

  let downloadName = sprintf "Altseed2-%s" altseed2CommitId

  let rec getArchiveUrl page = async {
    Trace.tracefn "page %d" page
    let! data = client.GetStringAsync(sprintf "%s?page=%d" url page) |> Async.AwaitTask

    let artifacts =
      data
      |> Json.deserialize<{| artifacts: {| name: string; archive_download_url: string; expired: bool |} [] |}>

    if artifacts.artifacts |> Array.isEmpty then
      failwithf "'%s' is not found" downloadName

    match
      artifacts.artifacts
      |> Seq.tryFind(fun x -> x.name = downloadName) with
    | Some x when x.expired -> return failwithf "'%s' is expired" downloadName
    | Some x -> return x.archive_download_url
    | None -> return! getArchiveUrl (page + 1)
  }

  let outputFilePath = sprintf "%s.zip" altseed2Dir

  async {
    let! archiveUrl = getArchiveUrl 1

    let! res =
      client.GetAsync(archiveUrl, Net.Http.HttpCompletionOption.ResponseHeadersRead)
      |> Async.AwaitTask

    use fileStream = IO.File.Create(outputFilePath)
    use! httpStream = res.Content.ReadAsStreamAsync() |> Async.AwaitTask
    do! httpStream.CopyToAsync(fileStream) |> Async.AwaitTask
    do! fileStream.FlushAsync() |> Async.AwaitTask
  } |> Async.RunSynchronously

  Zip.unzip altseed2Dir outputFilePath
)

Target.create "CISetting" (fun _ ->
  """module ResourcesPassword

module Server =
  let [<Literal>] url = @""
  let [<Literal>] username = @""
  let [<Literal>] password = @""
  let [<Literal>] tableTime5000  = @""
  let [<Literal>] tableScore180  = @""

let [<Literal>] password = @""

// 16
let [<Literal>] iv = @""

// 32
let [<Literal>] key = @""

"""
  |> File.writeString false "ResourcesPassword.fs"
)

Target.create "All" ignore

"Resources"
  ==> "Publish"

"Build"
  ==> "All"

Target.runOrDefault "All"
