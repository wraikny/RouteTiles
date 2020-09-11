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

let testProjects = []

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

let dotnet cmd arg = DotNet.exec id cmd arg |> ignore

Target.create "Tool" (fun _ ->
  dotnet "tool" "update paket"
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
  // ++ "lib/**/*.*proj"
  |> Seq.iter (DotNet.build id)
)

let runtimes = [ "linux-x64"; "win-x64"; "osx-x64" ]
let publishOutput = sprintf "publish/RouteTiles.%s"

let resources = "Resources"

Target.create "CopyShader" (fun _ ->
  Shell.copyDir (sprintf "%s/Shader" resources) @"src/Shader" (fun _ -> true)
)

Target.create "Resources" (fun _ ->
  let targetProject = "RouteTiles"

  !!(sprintf "%s/**" resources)
  |> Zip.zip resources (sprintf "%s.zip" resources)

  let outDir x = sprintf "src/%s/bin/%s/netcoreapp3.1" targetProject x

  // for Debug
  let dir = outDir "Debug"
  let target = sprintf "%s/%s" dir resources
  Directory.ensure dir
  Directory.delete target |> ignore
  Shell.copyDir target resources (fun _ -> true)
  // Shell.copyFile (sprintf "%s.pack" target) (sprintf "%s.pack" resources)
  Trace.trace "Finished Copying Resources for Debug"

  dotnet "fsi" "--exec pack.fsx"

  let packedResources = sprintf "%s.pack" resources

  [
    yield!
      runtimes
      |> Seq.map (fun r ->
        sprintf "%s/%s" (publishOutput r) packedResources
      )
    yield outDir "Release"
  ] |> Seq.iter(fun target ->
    Directory.ensure target
    packedResources
    |> Shell.copyFile target
  )
)

Target.create "Publish" (fun _ ->
  runtimes
  |> Seq.iter (fun target ->
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
          OutputPath = publishOutput target |> Some
      }
    )
  )
)

let altseed2Dir = @"lib/Altseed2"

Target.create "CopyLib" (fun _ ->
  let outputDirs = [
    @"lib/Altseed2.BoxUI/lib/Altseed2"
  ]
  outputDirs |> Seq.iter (fun dir ->
    Shell.copyDir dir altseed2Dir (fun _ -> true)
  )
)

Target.create "Download" (fun _ ->
  let commitId = "f3d5c7b1a0698a594823604da191f7457ce0be6a"

  let token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
  let url = @"https://api.github.com/repos/altseed/altseed2-csharp/actions/artifacts"

  use client = new Net.Http.HttpClient()
  client.DefaultRequestHeaders.UserAgent.ParseAdd("wraikny.RouteTiles")
  client.DefaultRequestHeaders.Authorization <- Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)

  let downloadName = sprintf "Altseed2-%s" commitId

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
  let [<Literal>] url = ""
  let [<Literal>] username = ""
  let [<Literal>] password = ""
  let [<Literal>] tableTime2000  = @""
  let [<Literal>] tableTime5000  = @""
  let [<Literal>] tableTime10000 = @""
  let [<Literal>] tableScore180  = @""
  let [<Literal>] tableScore300  = @""
  let [<Literal>] tableScore600  = @""

let [<Literal>] password = ""
"""
  |> File.writeString false "ResourcesPassword.fs"
)

Target.create "All" ignore

"CopyShader" ==> "Resources"

"Clean"
  ==> "Build"
  ==> "All"

Target.runOrDefault "All"
