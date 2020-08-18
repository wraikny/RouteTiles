module RouteTiles.App.Program

open System.Threading

open Altseed2
open RouteTiles.Core
open RouteTiles.Core.Types

open RouteTiles.App.Consts.ViewCommon

[<EntryPoint; System.STAThread>]
let main _ =
  let inline init(config) =
    if not <| Engine.Initialize("RouteTiles", windowSize.X, windowSize.Y, config) then
      failwith "Failed to initialize the Altseed2"

    Engine.ClearColor <- clearColor

  let initResources() =
    let loadingSize = windowSize.To2F() * Vector2F(0.75f, 0.25f)
    let loading = Loading(loadingSize, 0, 1)
    loading.Position <- (Engine.WindowSize.To2F() - loadingSize) * 0.5f

    let (menuElementCount, initMenuElement) =
      MenuElement.initialize loading.SetProgress

    let (menuTexturesCount, initMenuTextures) =
      MenuParams.Texture.initialize ((+) menuElementCount >>loading.SetProgress)

    let progressSum =
      menuElementCount
      + menuTexturesCount

    loading.Init(progressSum)

    Engine.AddNode(loading)

    async {
      let ctx = SynchronizationContext.Current

      do! initMenuElement
      do! initMenuTextures

      if SynchronizationContext.Current <> ctx then
        do! Async.SwitchToContext ctx
      let node = Menu()
      Engine.RemoveNode(loading)
      Engine.AddNode(node)
    }
    |> Async.StartImmediate

  let rec loop() =
    if Engine.DoEvents() then
      printfn "%f" Engine.DeltaSecond
      BoxUI.BoxUISystem.Update()
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

  initResources()

  // let controller =
  //   Engine.Joystick.GetJoystickInfo(0)
  //   |> function
  //   | info when isNull info -> Controller.Keyboard
  //   | info ->
  //     let name = if info.IsGamepad then info.GamepadName else info.Name
  //     Controller.Joystick(name, 0)

  // (
  //   let node = Game(SoloGame.Mode.TimeAttack, controller)
  //   Engine.AddNode(node)
  // )

  loop()

  BoxUI.BoxUISystem.Terminate()
  Engine.Terminate()
#else

  try
    init(config)

    try

      // Altseed2のバグでPackageから読み込めない……><
      if not <| Engine.File.AddRootPackageWithPassword(@"Resources.pack", ResourcesPassword.password) then
        failwithf "Failed to add root package"

      initResources()

      // (
      //   let node = Game(SoloGame.Mode.TimeAttack, Controller.Keyboard)
      //   Engine.AddNode(node)
      // )

      loop()
    with e ->
      Engine.Log.Error(LogCategory.User, sprintf "%A: %s" (e.GetType()) e.Message)
      reraise()

    BoxUI.BoxUISystem.Terminate()
    Engine.Terminate()
  with e ->
    printfn "%A: %s" (e.GetType()) e.Message
#endif

  0 // return an integer exit code
