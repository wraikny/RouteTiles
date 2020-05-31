module RouteTiles.App.Program

open Altseed

[<EntryPoint>]
let main _ =
  try
    let config =
      Configuration(
        FileLoggingEnabled = true,
        LogFileName = "Log.txt"
      )

#if DEBUG
    config.ConsoleLoggingEnabled <- true
#endif

    if not <| Engine.Initialize("RouteTiles", 1280, 720, config) then
      failwith "Failed to initialize the Altseed"

    Engine.ClearColor <- Color(200, 200, 200, 255)

#if DEBUG
    if not <| Engine.File.AddRootDirectory(@"Resources") then
      failwithf "Failed to add root directory %s" "Resources"
#endif

    Engine.AddNode(Game())

    let rec loop() =
      if Engine.DoEvents() then
        Engine.Update() |> ignore
        loop()

    loop()

    Engine.Terminate()
  with e ->
    printfn "%A: %s" (e.GetType()) e.Message

  0 // return an integer exit code
