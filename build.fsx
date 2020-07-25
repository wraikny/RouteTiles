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
          Common =
            { DotNet.Options.Create() with
                CustomParams = Some "-p:PublishSingleFile=true -p:PublishTrimmed=true" }
          Runtime = Some target
          Configuration = DotNet.BuildConfiguration.Release
          SelfContained = Some true
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

  let setHeader (h: Net.Http.Headers.HttpRequestHeaders) =
    h.UserAgent.ParseAdd("wraikny.RouteTiles")
    h.Authorization <- Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)

  let artifacts =
    url
    |> Http.getWithHeaders null null setHeader
    |> snd
    |> Json.deserialize<
      {|
        artifacts :
          {|
            name: string;
            archive_download_url: string;
            created_at: DateTime;
          |} []
      |}>

  let downloadName = sprintf "Altseed2-%s" commitId
  
  let downloadTarget =
    artifacts.artifacts
    |> Seq.find(fun x -> x.name = downloadName)
  
  use client = new Net.Http.HttpClient()
  client.DefaultRequestHeaders |> setHeader

  let outputFilePath = sprintf "%s.zip" outputPath

  async {
    let! res =
      client.GetAsync(downloadTarget.archive_download_url, Net.Http.HttpCompletionOption.ResponseHeadersRead)
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
