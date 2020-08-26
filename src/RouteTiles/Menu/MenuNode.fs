namespace RouteTiles.App

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI

open RouteTiles.Core.Types
open RouteTiles.Core.Types.Menu
open RouteTiles.Core.Menu
open RouteTiles.Core.Effects

open RouteTiles.Core.Utils



module Config =
  open System.IO
  open System.Collections.Concurrent
  open System.Runtime.Serialization
  open System.Runtime.Serialization.Formatters.Binary

  let [<Literal>] ConfigFile = @"Data/config.bat"

  let dirName = Path.GetDirectoryName ConfigFile
  let formatter = new BinaryFormatter()

  let private write (conf: Config) =
    let exists = Directory.Exists(dirName)
    if not exists then
      Directory.CreateDirectory(dirName) |> ignore

    use file = new FileStream(ConfigFile, FileMode.Create)
    formatter.Serialize(file, conf)

  let private writeQueue = ConcurrentQueue<Config>()

  let save = writeQueue.Enqueue

  let update = async {
    let ctx = SynchronizationContext.Current
    while true do
      match writeQueue.TryDequeue() with
      | true, conf ->
        do! Async.SwitchToThreadPool()
        write conf
        do! Async.SwitchToContext ctx
      | _ -> do! Async.Sleep 100
  }

  let mutable private config = ValueNone
  let tryGetConfig() = config

  let initialize =
    1, fun (progress: unit -> int) -> async {
      do! Async.SwitchToThreadPool()

      let exists = Directory.Exists(dirName)
      if not exists then
        Directory.CreateDirectory(dirName) |> ignore

      let fileExists = File.Exists(ConfigFile)

      let createNew() =
        let config' = Config.Create()
        save config'
        config <- ValueSome config'

      if fileExists then
        try
          use file = new FileStream(ConfigFile, FileMode.OpenOrCreate)
          config <- formatter.Deserialize(file) |> unbox<Config> |> ValueSome
        with :? SerializationException ->
          createNew()
      else
        createNew()

      progress() |> ignore
    }


module RankingServer =
  let client =
    new SimpleRankingsServer.Client(
      ResourcesPassword.Server.url,
      ResourcesPassword.Server.username,
      ResourcesPassword.Server.password
    )

  let urlMap = dict [|
    SoloGameModeStrict.TimeAttack2000,
    ResourcesPassword.Server.tableTime2000
    
    SoloGameModeStrict.TimeAttack5000,
    ResourcesPassword.Server.tableTime5000
    
    SoloGameModeStrict.TimeAttack10000,
    ResourcesPassword.Server.tableTime10000
    
    SoloGameModeStrict.ScoreAttack180,
    ResourcesPassword.Server.tableScore180
    
    SoloGameModeStrict.ScoreAttack300,
    ResourcesPassword.Server.tableScore300
    
    SoloGameModeStrict.ScoreAttack600,
    ResourcesPassword.Server.tableScore600
  |]


module MenuUtil =
  let getCurrentControllers() =
    [|
      yield Controller.Keyboard
      for i in 0..15 do
        let info = Engine.Joystick.GetJoystickInfo i
        if info <> null && info.IsGamepad then
          yield Controller.Joystick(i, info.GamepadName, info.GUID)
    |]


open EffFs

[<Struct>]
type MenuHandler = {
  Dispatch: Menu.Msg -> unit
  Pause: unit -> unit
  Resume: unit -> unit
  QuitGame: unit -> unit
  Restart: unit -> unit
  StartGame: SoloGame.Mode * Controller -> unit
} with
  static member inline Handle(x) = x

  static member inline Handle(e: SoundEffect, k) =
    printfn "Effect: %A" e
    k()

  static member inline Handle(e: GameControlEffect, k) =
    printfn "Effect: %A" e
    Eff.capture(fun h ->
      e |> function
      | GameControlEffect.Pause -> h.Pause()
      | GameControlEffect.Resume -> h.Resume()
      | GameControlEffect.Quit -> h.QuitGame()
      | GameControlEffect.Restart -> h.Restart()
      k()
    )

  static member inline Handle(CurrentControllers as e, k) =
    printfn "Effect: %A" e
    MenuUtil.getCurrentControllers() |> k

  static member inline Handle(GameStartEffect(x,y) as e, k) =
    printfn "Effect: %A" e
    Eff.capture(fun h -> h.StartGame(x, y); k() )

  static member inline Handle(GameRankingEffect param as e, k) =
    printfn "Effect: %A" e
    Eff.capture(fun h ->
      async {
        let orderKey, isDescending = param.mode |> function
          | SoloGame.Mode.TimeAttack _ -> "Time", false
          | SoloGame.Mode.ScoreAttack _ -> "Point", true

        let table = RankingServer.urlMap.[param.mode |> SoloGameModeStrict.From]

        let! result =
          async {
            let! id = RankingServer.client.AsyncInsert(table, param.guid, param.result)
            let! data = RankingServer.client.AsyncSelect<GameResult>(table, orderBy = orderKey, isDescending = isDescending, limit = 10)
            // let data =
            //   data
            //   |> Array.filter (fun x -> x.values.Kind = param.result.Kind)
            let data = if data.Length > 10 then data.[0..9] else data
            return (id, data)
          }
          |> Async.Catch
        match result with
        | Choice1Of2 (id, data) ->
          h.Dispatch(Msg.RankingResult <| Ok(id, data))
        | Choice2Of2 e ->
          h.Dispatch(Msg.RankingResult <| Error e.Message)
        
      } |> Async.StartImmediate
      k()
    )

  static member inline Handle(SaveConfig config as e, k) =
    printfn "Effect: %A" e
    Config.save config
    k()

type MenuNode() =
  inherit Node()
  let mutable prevModel = ValueNone
  let updater = Updater<Model, Msg>()

  // let coroutine = CoroutineNode()
  let uiRoot = BoxUIRootNode()

  let mutable gameNode = ValueNone

  do
    // base.AddChildNode coroutine
    base.AddChildNode uiRoot

    updater.Subscribe(fun model ->
      prevModel
      |> function
      | ValueSome x when x = model -> ()
      | _ ->
        prevModel <- ValueSome model
        uiRoot.ClearElement()

        let (elem) = MenuView.menu model

        uiRoot.SetElement elem
    ) |> ignore

  let mutable prevControllerCount = Engine.Joystick.ConnectedJoystickCount

  let getKeyboardInput = InputControl.getKeyboardInput InputControl.Menu.keyboard
  let getJoystickInput = InputControl.getJoystickInput InputControl.Menu.joystick

  let playerInput() =
    getKeyboardInput ()
    |> Option.alt(fun () ->
      let count = Engine.Joystick.ConnectedJoystickCount
      seq {
        for i in 0..count-1 do
          let info = Engine.Joystick.GetJoystickInfo(i)
          if info <> null && info.IsGamepad then
            match getJoystickInput i with
            | Some x -> yield x
            | _ -> ()
      }
      |> Seq.tryHead
    )

  override this.OnUpdate() =
    // Controllerの接続状況の更新
    if updater.Model.Value.state.ControllerRefreshEnabled then
      let curretConnectedCount = Engine.Joystick.ConnectedJoystickCount
      if curretConnectedCount <> prevControllerCount then
        prevControllerCount <- curretConnectedCount
        let controllers = MenuUtil.getCurrentControllers()
        updater.Dispatch(Msg.RefreshController controllers) |> ignore

    // 入力受付
    updater.Model.Value.state
    |> function
    | State.Game (_, controller) ->
      if InputControl.pauseInput controller then
        updater.Dispatch(Msg.Pause)

    | s when s.IsStringInputMode ->
      InputControl.getKeyboardInput InputControl.Menu.characterInput ()
      |> Option.iter(updater.Dispatch)

    | _ ->
      playerInput()
      |> Option.iter (updater.Dispatch)

  override this.OnAdded() =
    let handler = {
      Dispatch = updater.Dispatch
      Pause = fun () -> Engine.Pause(this)

      Resume = fun () -> Engine.Resume()

      StartGame = fun (gameMode, controller) ->
        gameNode |> function
        | ValueNone ->
          // todo
          let gameInfo = GameInfoNode(Position = Helper.SoloGame.gameInfoCenterPos)
          let viewer = { new IGameInfoViewer with
            member __.SetPoint(m, p) = gameInfo.SetPoint(m, p)
            member __.SetTime(t) = gameInfo.SetTime(t)
            member __.FinishGame(model, t) =
              updater.Dispatch(Msg.FinishGame(model, t))
              Engine.Pause(this)
          }
          let n = Game(gameMode, controller, viewer)
          n.AddChildNode(gameInfo)

          Engine.AddNode(n)

          gameNode <- ValueSome n

        | ValueSome n ->
          Engine.AddNode(n)

      QuitGame = fun () ->
        gameNode |> function
        | ValueSome n ->
          Engine.RemoveNode(n)
        | ValueNone ->
          failwith "invalid state for QuitGame"

      Restart = fun () ->
        gameNode |> function
        | ValueSome n -> n.Initialize()
        | ValueNone ->
          failwith "invalid state for QuitGame"
    }

    let update msg model =
      let newModel =
        update msg model
        |> Eff.handle handler
#if DEBUG
      printfn "Msg: %A\nModel: %A\n" msg newModel
#endif
      newModel

    let config = Config.tryGetConfig().Value

    prevModel <-
      (initModel config, update)
      |> updater.Init
      |> ValueSome
