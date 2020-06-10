#r "netstandard"
#r "./lib/Altseed2/Altseed2.dll"

open Altseed

let target = "Resources"

if Engine.Initialize("Pack", 1, 1, Configuration(ConsoleLoggingEnabled=true)) then

  Engine.File.Pack(target + "/", target + ".pack")
  |> printfn "Pack result: %A"

  Engine.Terminate()
else
  printfn "Failed to initialize"
