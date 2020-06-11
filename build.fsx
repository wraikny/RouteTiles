#if !FAKE
#load ".fake/build.fsx/intellisense.fsx"
#r "netstandard"
#endif

open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

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

Target.create "CI" (fun _ ->
  "src/RouteTiles.Core/RouteTiles.Core.fsproj"
  |> DotNet.build id
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"

"Tool"

Target.runOrDefault "All"
