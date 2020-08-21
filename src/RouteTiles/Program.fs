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

  let initializers = [|
    Consts.initialize
    MenuView.initialize
  |]

  let initResources() =
    Engine.AddNode(PostEffect.Wave(ZOrder = ZOrder.posteffect))
    
    let progressSum = initializers |> Seq.sumBy fst

    let loadingSize = windowSize.To2F() * Vector2F(0.75f, 0.125f)
    let loading =
      Loading(progressSum, loadingSize, 0, 1,
        Position = (Engine.WindowSize.To2F() - loadingSize) * 0.5f
      )

    let loader =
      initializers
      |> Array.map(fun (_, i) -> i loading.Progress)
      |> Async.Parallel
      |> Async.Ignore

    Engine.AddNode(loading)

    async {
      let ctx = SynchronizationContext.Current

      do! loader
      do! Async.SwitchToContext ctx

      let node = MenuNode()
      Engine.RemoveNode(loading)
      Engine.AddNode(node)
    }
    |> Async.StartImmediate

  let rec loop() =
    if Engine.DoEvents() then
      // printfn "%f" Engine.DeltaSecond
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
  loop()

  BoxUI.BoxUISystem.Terminate()
  Engine.Terminate()
#else

  try
    init(config)

    try
      if not <| Engine.File.AddRootPackageWithPassword(@"Resources.pack", ResourcesPassword.password) then
        failwithf "Failed to add root package"

      initResources()
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
