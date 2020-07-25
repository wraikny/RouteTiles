#if !FAKE
#load ".fake/build.fsx/intellisense.fsx"
#r "netstandard"
#endif

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
  ++ "publish"
  |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
  !! "src/**/*.*proj"
  ++ "tests/**/*.*proj"
  |> Seq.iter (DotNet.build id)
)

let runtimes = [ "linux-x64"; "win-x64"; "osx-x64" ]
let publishOutput = sprintf "publish/RouteTiles.%s"

Target.create "Resources" (fun _ ->
  let targetProject = "RouteTiles"
  let resources = "Resources"
  // let password = Some "password"

  !!(sprintf "%s/**" resources)
  |> Zip.zip resources (sprintf "%s.zip" resources)

  let outDir x = sprintf "src/%s/bin/%s/netcoreapp3.1" targetProject x

  // for Debug
  let dir = outDir "Debug"
  let target = sprintf "%s/%s" dir resources
  Directory.create dir
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


Target.create "Download" (fun _ ->
  let commitId = "c05605fffaaed70b81c8a09c2ac108b8a57c9452"

  let token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
  let url = @"https://api.github.com/repos/altseed/altseed2-csharp/actions/artifacts"

  let outputPath = @"lib/Altseed2"

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

  let outputFilePath = sprintf "%s.zip" outputPath

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

  Zip.unzip outputPath outputFilePath
)

Target.create "CISetting" (fun _ ->
  let password = "fakepassword"

  sprintf """module ResourcesPassword
  [<Literal>] let password = "%s"
"""
    password
  |> File.writeString false "ResourcesPassword.fs"
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"

"Publish"
  ==> "Resources"

"Tool"

Target.runOrDefault "All"
