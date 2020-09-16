namespace RouteTiles.App.MenuV2

open System
open Altseed2
open Altseed2.BoxUI

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects
open RouteTiles.App

open EffFs

module RankingServer =
  let client =
    new SimpleRankingsServer.Client(
      ResourcesPassword.Server.url,
      ResourcesPassword.Server.username,
      ResourcesPassword.Server.password
    )

  type Param = {
    mode: MenuV2.GameMode
    table: string
    guid: Guid
    result: MenuV2.GameResult
    limit: int
  }

  let insertSelect (param: Param) =
    let orderKey, isDescending = param.mode |> function
      | MenuV2.GameMode.TimeAttack2000 -> "Time", false
      | MenuV2.GameMode.ScoreAttack180 -> "Point", true
    async {
      let! id = client.AsyncInsert(param.table, param.guid, param.result)
      let! data = client.AsyncSelect<MenuV2.GameResult>(param.table, orderBy = orderKey, isDescending = isDescending, limit = param.limit)
      return (id, data)
    } |> Async.Catch


module MenuUtil =
  let getCurrentControllers() =
    [|
      yield Controller.Keyboard
      for i in 0..15 do
        let info = Engine.Joystick.GetJoystickInfo i
        if info <> null && info.IsGamepad then
          yield Controller.Joystick(i, info.GamepadName, info.GUID)
    |]


type MenuV2Handler = {
  dispatch: MenuV2.Msg -> unit
  handleGameControlEffect: GameControlEffect -> unit
} with
  static member Handle(x) = x

  static member inline Handle(e, k) = Library.StateMachine.handle (e, k)

  static member inline Handle(e: SoundEffect, k) =
    Utils.DebugLogn (sprintf "SoundEffect: %A" e)
    k ()

  static member inline Handle(e: GameControlEffect, k) =
    Utils.DebugLogn (sprintf "GameControlEffect: %A" e)
    Eff.capture (fun h ->
      h.handleGameControlEffect e
      k()
    )

  static member inline Handle(CurrentControllers as e, k) =
    Utils.DebugLogn (sprintf "Effect: %A" e)
    MenuUtil.getCurrentControllers() |> k

  static member inline Handle(SaveConfig config as e, k) =
    Utils.DebugLogn (sprintf "Effect: %A" e)
    Config.save config
    k()


type MenuV2Node() =
  inherit Node()

  let mutable lastState = ValueNone
  let updater = Updater<MenuV2.State, MenuV2.Msg>()

  let uiRoot = BoxUIRootNode()
  let uiRootModal = BoxUIRootNode()

  let mutable gameNode = ValueNone

  do
    base.AddChildNode uiRoot
    base.AddChildNode uiRootModal


    let container = Container(TextMap.textMapJapanese)

    updater.Subscribe (fun state ->
      lastState |> function
      | ValueSome x when MenuV2.equal x state -> ()
      | _ ->
        lastState <- ValueSome state

        uiRootModal.ClearElement()
        MenuModalElement.createModal container state
        |> function
        | ValueSome elem ->
          uiRootModal.SetElement elem
        | _ ->
          uiRoot.ClearElement()
          let elem = MenuElement.create container state
          uiRoot.SetElement elem
    ) |> ignore

  let mutable lastControllerCount = Engine.Joystick.ConnectedJoystickCount

  let getKeyboardInput = InputControl.getKeyboardInput InputControl.MenuV2.keyboard
  let getJoystickInput = InputControl.getJoystickInput InputControl.MenuV2.joystick
  let getCharacterInput = InputControl.getKeyboardInput InputControl.MenuV2.characterInput

  let getJoysticksInputs() =
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

  let getInput() =
    getKeyboardInput ()
    |> Option.alt getJoysticksInputs

  override this.OnAdded() =
    let handler = {
      dispatch = updater.Dispatch
      handleGameControlEffect = function
        | GameControlEffect.Pause -> Engine.Pause(this)
        | GameControlEffect.Resume -> Engine.Resume()
        | GameControlEffect.Start(gameMode, controller) ->
          gameNode |> function
          | ValueSome n -> Engine.AddNode(n)
          | ValueNone ->
            let gameInfo = GameInfoNode(Position = Helper.SoloGame.gameInfoCenterPos)
            let viewer = { new IGameHandler with
              member __.SetPoint(m, p) = gameInfo.SetPoint(m, p)
              member __.SetTime(t) = gameInfo.SetTime(t)
              member __.FinishGame(model, t) =
                MenuV2.Msg.FinishGame(model, t)
                |> updater.Dispatch
              member __.SelectController() = ()
            }
            let n = Game(gameMode, controller, viewer)
            n.AddChildNode(gameInfo)

            Engine.AddNode(n)

            gameNode <- ValueSome n

        | GameControlEffect.Quit ->
          gameNode |> ValueOption.iter Engine.RemoveNode

        | GameControlEffect.Restart ->
          gameNode |> ValueOption.iter (fun n -> n.Initialize())

        | GameControlEffect.SetController(controller) ->
          gameNode.Value.Controller <- controller
    }

    let config = Config.tryGetConfig().Value

    updater.Init(MenuV2.State.Init (config), fun msg state ->
      let newState = MenuV2.update msg state |> Eff.handle handler
      Utils.DebugLogn (sprintf "Msg: %A\nState: %A\n" msg newState)
      newState
    )

    lastState <- updater.Model

  override this.OnUpdate() =
    updater.Model.Value |> function
    | MenuV2.GameState (_, controller, _) ->
      if InputControl.pauseInput controller then
        updater.Dispatch MenuV2.Msg.PauseGame

    | MenuV2.SettingMenuState(SubMenu.Setting.State.InputName _, _) ->
      getCharacterInput ()
      |> Option.alt getJoysticksInputs
      |> Option.iter updater.Dispatch

    | MenuV2.ControllerSelectState _ ->
      let curretConnectedCount = Engine.Joystick.ConnectedJoystickCount
      if curretConnectedCount <> lastControllerCount then
        lastControllerCount <- curretConnectedCount
        let controllers = MenuUtil.getCurrentControllers()
        updater.Dispatch(MenuV2.UpdateControllers controllers) |> ignore

      getInput()
      |> Option.iter updater.Dispatch

    | _ ->
      getInput()
      |> Option.iter updater.Dispatch
