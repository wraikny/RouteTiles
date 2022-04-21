#r "netstandard"
#r "nuget: Altseed2, 2.2.2"
#load "ResourcesPassword.fs"
#load "src/RouteTiles/TextMap.fs"

open System
open System.Reflection
open Altseed2
open RouteTiles.App

let getProperties (x: 'a) =
  typeof<'a>.GetProperties(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance)
  |> Array.map(fun p -> p.GetValue(x, null))

let getStringPropertyCharacters (x: 'a) =
  [|for v in getProperties x do
      match v with
      | :? string as v -> yield! v
      | _ -> eprintfn "%O is not string" v
  |]

let alphabetAndNums = [|
  for c in 'a'..'z' -> c
  for c in 'A'..'Z' -> c
  for c in '0'..'9' -> c
|]

let fonts = [|
  ( @"Makinas-4-Square.otf"
  , ([|
        yield! alphabetAndNums

        yield!
          TextMap.textMapJapanese.buttons
          |> getStringPropertyCharacters

        yield!
          TextMap.textMapJapanese.descriptions
          |> getStringPropertyCharacters

        yield!
          TextMap.textMapJapanese.modes
          |> getStringPropertyCharacters
        yield!
          TextMap.textMapJapanese.gameInfo
          |> getStringPropertyCharacters

        yield! TextMap.textMapJapanese.others

    |] |> Array.distinct |> fun cs -> String(cs))
  )
|]

let fontDir = "Fonts"
let target = "Resources"

let coreModules = CoreModules.File ||| CoreModules.Graphics

if not <| Engine.Initialize("Pack", 1, 1, Configuration(ConsoleLoggingEnabled=true, EnabledCoreModules = coreModules)) then
  failwith "Failed to initialize the Engine"

for (fontname, chars) in fonts do
  let inPath = sprintf "%s/%s" fontDir fontname
  let outDir = sprintf "%s/Font/%s" target (IO.Path.GetFileNameWithoutExtension (fontname))
  let outPath = sprintf "%s/font.a2f" outDir

  if IO.Directory.Exists(outDir) then
    IO.Directory.Delete(outDir, true)

  IO.Directory.CreateDirectory(outDir) |> ignore

  let msg =
    sprintf """GenerateFontFile
InPath: %s
OutPath: %s
characters:%s
"""
      inPath outPath chars

  printfn "%s" msg

  if Font.GenerateFontFile(inPath, outPath, chars, 48) then
    printfn "Success!"
  else
    failwith msg

printfn "Packing %s to %s.pack ..." target target
if Engine.File.PackWithPassword(target + "/", target + ".pack", ResourcesPassword.password) then
  printfn "Success!"
else
  failwith "Failed to packing resources"

Engine.Terminate()
