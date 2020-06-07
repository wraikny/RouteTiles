module RouteTiles.App.Program

open Altseed


[<EntryPoint>]
let main _ =
  let inline init(config) =
    if not <| Engine.Initialize("RouteTiles", Consts.windowSize.X, Consts.windowSize.Y, config) then
      failwith "Failed to initialize the Altseed"

    Engine.ClearColor <- Consts.clearColor

    Engine.Joystick.RefreshConnectedState()

  let rec loop() =
    if Engine.DoEvents() then
      Engine.Update() |> ignore
      loop()

  let config =
    Configuration(
      FileLoggingEnabled = true,
      LogFileName = "Log.txt"
    )

#if DEBUG
  config.ConsoleLoggingEnabled <- true

  init(config)

  if not <| Engine.File.AddRootDirectory(@"Resources") then
    failwithf "Failed to add root directory"

  Engine.AddNode(Game())

  loop()

  Engine.Terminate()
#else

  try
    init(config)

    if not <| Engine.File.AddRootPackage(@"Resources.pack") then
      failwithf "Failed to add root package"

    Engine.AddNode(Game())

    loop()

    Engine.Terminate()
  with e ->
    printfn "%A: %s" (e.GetType()) e.Message
#endif

  0 // return an integer exit code
