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
  StartGame: SoloGame.Mode * Controller -> unit
  QuitGame: unit -> unit
} with
  static member inline Handle(x) = x

  static member inline Handle(_kind: SoundEffect, k) =
    k()

  static member inline Handle(e: GameControlEffect, k) =
    Eff.capture(fun h ->
      e |> function
      | GameControlEffect.Pause -> h.Pause()
      | GameControlEffect.Resume -> h.Resume()
      | GameControlEffect.Quit -> h.QuitGame()
      | GameControlEffect.Restart -> ()
      k()
    )

  static member inline Handle(CurrentControllers, k) =
    MenuUtil.getCurrentControllers() |> k

  static member inline Handle(GameStartEffect(x,y), k) =
    Eff.capture(fun h -> h.StartGame(x, y); k() )

  static member inline Handle(GameRankingEffect param, k) =
    Eff.capture(fun h ->
      h.Dispatch(Msg.RankingResult(Error "未実装です。"))
      k()
    )

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

    | State.GameResult (_, _, GameRankingState.InputName _) ->
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
            member __.FinishGame(p, t) =
              updater.Dispatch(Msg.FinishGame(p, t))
              Engine.Pause(this)
          }
          let n = Game(gameMode, controller, viewer)
          n.AddChildNode(gameInfo)

          Engine.AddNode(n)

          gameNode <- ValueSome n

        | ValueSome n ->
          n.Initialize()
          Engine.AddNode(n)

      QuitGame = fun () ->
        gameNode |> function
        | ValueSome n ->
          n.Parent.RemoveChildNode(n)
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

    prevModel <-
      (initModel, update)
      |> updater.Init
      |> ValueSome
