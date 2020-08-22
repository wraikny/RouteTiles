module RouteTiles.App.Program

open System.Threading

open Altseed2
open RouteTiles.Core
open RouteTiles.Core.Types

open RouteTiles.App.Consts.ViewCommon

[<EntryPoint; System.STAThread>]
let main _ =
  let inline init(config) =
    Engine.InitializeEx("RouteTiles", windowSize, config)

    Engine.ClearColor <- clearColor

  let initializers = [|
    Config.initialize
#if !DEBUG
    Consts.initialize
    MenuView.initialize
#endif
  |]

  let initLoading() =
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
      do! loader
      Engine.RemoveNode(loading)
    }

  let initGame() =
    Engine.AddNode(PostEffect.Wave(ZOrder = ZOrder.posteffect))

    async {
      do! initLoading()
      let! _ = Async.StartChild (Config.update)
      Engine.AddNode(MenuNode())
    }
    |> Async.StartImmediate

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

  initGame()
  Engine.Run()
  Engine.TerminateEx()
#else

  try
    init(config)

    try
      if not <| Engine.File.AddRootPackageWithPassword(@"Resources.pack", ResourcesPassword.password) then
        failwithf "Failed to add root package"

      initGame()
      Engine.Run()

    with e ->
      Engine.Log.Error(LogCategory.User, sprintf "%A: %s" (e.GetType()) e.Message)
      reraise()

    Engine.TerminateEx()
  with e ->
    printfn "%A: %s" (e.GetType()) e.Message
#endif

  0 // return an integer exit code
