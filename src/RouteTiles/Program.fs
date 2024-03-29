﻿module RouteTiles.App.Program

open System.Threading
open System.IO

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

    let container = Menu.Container(TextMap.textMapJapanese)

    let progressSum = 1 + container.ProgressCount + soundProgress
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
        container.Load(loading.Progress >> ignore)

        do! soundInit loading.Progress

        return container
      })

      let! _ = Async.StartChild Config.update
      let! _ = Async.StartChild ErrorLog.update
      let! container = child

      Engine.RemoveNode loading
      Engine.AddNode (Menu.MenuNode(config, container))
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
    File.AppendAllText(CriticalErrorLogFile, ErrorLog.toString e)
    reraise()
#endif

  0 // return an integer exit code
