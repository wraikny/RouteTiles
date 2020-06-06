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

    if not <| Engine.Initialize("RouteTiles", Consts.windowSize.X, Consts.windowSize.Y, config) then
      failwith "Failed to initialize the Altseed"

    Engine.ClearColor <- Color(200, 200, 200, 255)

#if DEBUG
    if not <| Engine.File.AddRootDirectory(@"Resources") then
      failwithf "Failed to add root directory"
#else
    if not <| Engine.File.AddRootPackage(@"Resources.pack") then
      failwithf "Ffailed to add root package"
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
