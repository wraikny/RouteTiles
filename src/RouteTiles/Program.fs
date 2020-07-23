module RouteTiles.App.Program

open Altseed2
open RouteTiles.Core
open RouteTiles.Core.Types


[<EntryPoint>]
let main _ =
  let inline init(config) =
    if not <| Engine.Initialize("RouteTiles", Consts.windowSize.X, Consts.windowSize.Y, config) then
      failwith "Failed to initialize the Altseed2"

    Engine.ClearColor <- Consts.clearColor

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

  let controller =
    Engine.Joystick.GetJoystickInfo(0)
    |> function
    | info when isNull info -> Controller.Keyboard
    | info ->
      let name = if info.IsGamepad then info.GamepadName else info.Name
      Controller.Joystick(name, 0)

  (
    let node = Game(SoloGame.Mode.TimeAttack, controller)
    Engine.AddNode(node)
  )


  loop()

  Engine.Terminate()
#else

  try
    init(config)

    try

      // Altseed2のバグでPackageから読み込めない……><
      if not <| Engine.File.AddRootPackageWithPassword(@"Resources.pack", ResourcesPassword.password) then
        failwithf "Failed to add root package"

      (
        let node = Game(SoloGame.Mode.TimeAttack, Controller.Keyboard)
        Engine.AddNode(node)
      )

      loop()
    with e ->
      Engine.Log.Error(LogCategory.User, sprintf "%A: %s" (e.GetType()) e.Message)
      reraise()

    Engine.Terminate()
  with e ->
    printfn "%A: %s" (e.GetType()) e.Message
#endif

  0 // return an integer exit code
