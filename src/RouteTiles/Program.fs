﻿module RouteTiles.App.Program

open System.Threading

open Altseed2
open RouteTiles.Core
open RouteTiles.Core.Types

open RouteTiles.App.Consts.ViewCommon

let [<Literal>] CriticalErrorLogFile = @"RouteTiles.CriticalError.log"

[<EntryPoint; System.STAThread>]
let private main _ =
  let inline init(config) =
    Engine.InitializeEx("RouteTiles", windowSize, config)

    Engine.ClearColor <- clearColor

  let initGame() =
    let soundProgress, soundInit = SoundControl.loading

    let progressSum = 1 + MenuV2.Container.ProgressCount + soundProgress
    let loadingSize = windowSize.To2F() * Vector2F(0.75f, 0.125f)
    let loading =
      Loading
        ( progressSum
        , loadingSize
        , 0
        , 1
        , Position = (Engine.WindowSize.To2F() - loadingSize) * 0.5f
        )
    
    Engine.AddNode loading

    async {
      let! config = Config.initialize
      loading.Progress() |> ignore

      let! child = Async.StartChild (async {
        let container = MenuV2.Container(TextMap.textMapJapanese, loading.Progress)

        do! soundInit loading.Progress

        return container
      })

      let! _ = Async.StartChild Config.update
      let! _ = Async.StartChild ErrorLog.update
      let! container = child

      Engine.RemoveNode loading
      Engine.AddNode (MenuV2.MenuV2Node(config, container))
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
    eprintfn "%O" e
    System.IO.File.AppendAllText(CriticalErrorLogFile, ErrorLog.toString e)
    reraise e
#endif

  0 // return an integer exit code
