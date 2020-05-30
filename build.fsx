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

Target.create "Publish" (fun _ ->
  [ "linux-x64"; "win-x64"; "osx-x64" ]
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
          OutputPath = sprintf "publish/RouteTiles.%s" target |> Some
      }
    )
  )
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"

"Tool"

Target.runOrDefault "All"
