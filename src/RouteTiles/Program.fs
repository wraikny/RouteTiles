module RouteTiles.App.Program

open System.Threading

open Altseed2
open RouteTiles.Core
open RouteTiles.Core.Types

open RouteTiles.App.Consts.ViewCommon

[<EntryPoint; System.STAThread>]
let private main _ =
  let inline init(config) =
    Engine.InitializeEx("RouteTiles", windowSize, config)

    Engine.ClearColor <- clearColor

  let initGame() =
    // Engine.AddNode(PostEffect.Wave(ZOrder = ZOrder.posteffect))

    async {
      let! config = Config.initialize
      let! _ = Async.StartChild (Config.update)
      Engine.AddNode (MenuV2.MenuV2Node(config))
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
