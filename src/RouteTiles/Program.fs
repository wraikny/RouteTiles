module RouteTiles.Program

open Altseed

[<EntryPoint>]
let main _ =
  let config =
    Configuration(
      ConsoleLoggingEnabled = true,
      FileLoggingEnabled = true,
      LogFileName = "Log.txt"
    )

  if Engine.Initialize("RouteTiles", 800, 600, config) then

    let rec loop() =
      if Engine.DoEvents() then
        Engine.Update() |> ignore
        loop()

    loop()

    Engine.Terminate()
  else
    printfn "Failed to initialize altseed."

  0 // return an integer exit code
