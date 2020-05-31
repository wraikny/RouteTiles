module RouteTiles.App.Program

open Altseed

[<EntryPoint>]
let main _ =
  let config =
    Configuration(
      FileLoggingEnabled = true,
      LogFileName = "Log.txt"
    )

  #if DEBUG
  config.ConsoleLoggingEnabled <- true
  #endif

  if Engine.Initialize("RouteTiles", 800, 600, config) then

    Engine.ClearColor <- Color(250, 255, 156, 255)

    Engine.AddNode(Board())

    let rec loop() =
      if Engine.DoEvents() then
        Engine.Update() |> ignore
        loop()

    loop()

    Engine.Terminate()
  else
    printfn "Failed to initialize altseed."

  0 // return an integer exit code
