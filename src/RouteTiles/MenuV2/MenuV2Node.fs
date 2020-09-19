namespace RouteTiles.App.MenuV2

open System
open Altseed2
open Altseed2.BoxUI

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects
open RouteTiles.App

open EffFs

module internal RankingServer =
  open ResourcesPassword

  let client =
    new SimpleRankingsServer.Client(
      #if DEBUG
      @"http://localhost:8080/api/RouteTiles",
      "username",
      "password"
      #else
      Server.url,
      Server.username,
      Server.password
      #endif
    )

  let insertSelect guid gameMode (data: RouteTiles.Core.Types.Ranking.Data) =
    let orderKey, isDescending, table = gameMode |> function
      | SoloGame.GameMode.TimeAttack2000 -> "Time", false, Server.tableTime2000
      | SoloGame.GameMode.ScoreAttack180 -> "Point", true, Server.tableScore180

    async {
      let! id = client.AsyncInsert(table, guid, data)
      let! data =
        client.AsyncSelect<RouteTiles.Core.Types.Ranking.Data>
          ( table
          , orderBy = orderKey
          , isDescending = isDescending
          , limit = 5
          )

      return (id, data)
    } |> Async.Catch


module internal MenuUtil =
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
  handleSetController: Controller -> bool
} with
  static member Handle(x) = x

  static member inline Handle(e, k) = Library.StateMachine.handle (e, k)

  static member Handle(e: SoundEffect, k) =
    Utils.DebugLogn (sprintf "SoundEffect: %A" e)
    k ()

  static member Handle(e: GameControlEffect, k) =
    Utils.DebugLogn (sprintf "GameControlEffect: %A" e)
    Eff.capture (fun h ->
      h.handleGameControlEffect e
      k()
    )

  static member Handle(SetController controller, k) =
    Utils.DebugLogn (sprintf "SetControllerEffect: %A" controller)
    Eff.capture ( fun h ->
      h.handleSetController controller |> k
    )

  static member Handle(CurrentControllers as e, k) =
    Utils.DebugLogn (sprintf "Effect: %A" e)
    MenuUtil.getCurrentControllers() |> k

  static member Handle(SaveConfig config as e, k) =
    Utils.DebugLogn (sprintf "Effect: %A" e)
    Config.save config
    k()

  static member Handle(GameRankingEffect (guid, gameMode, data), k) =
    Eff.capture(fun h ->
      async {
        let! res = RankingServer.insertSelect guid gameMode data
        let res = res |> function
          | Choice1Of2 x -> Ok x
          | Choice2Of2 e -> Error e
        h.dispatch <| MenuV2.Msg.ReceiveRanking res
      }
      |> Async.StartImmediate
      k ()
    )


type internal MenuV2Node(config: Config) =
  inherit Node()

  let mutable lastState = ValueNone
  let updater = Updater<MenuV2.State, MenuV2.Msg>()

  let uiRootMenu = BoxUIRootNode()
  let uiRootModal = BoxUIRootNode()

  let container = Container(TextMap.textMapJapanese)

  let mutable gameNode: Game voption = ValueNone

  do
    base.AddChildNode uiRootMenu
    base.AddChildNode uiRootModal

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
          uiRootMenu.ClearElement()
          let elem = MenuElement.create container state
          uiRootMenu.SetElement elem
    ) |> ignore

  let mutable lastControllerCount = Engine.Joystick.ConnectedJoystickCount

  let getJoysticksInputs() =
    let count = Engine.Joystick.ConnectedJoystickCount
    seq {
      for i in 0..count-1 do
        let info = Engine.Joystick.GetJoystickInfo(i)
        if info <> null && info.IsGamepad then
          match InputControl.MenuV2.getJoystickInput i with
          | Some x -> yield x
          | _ -> ()
    }
    |> Seq.tryHead

  let getInput() =
    InputControl.MenuV2.getKeyboardInput ()
    |> Option.orElseWith getJoysticksInputs

  let isAvailableController = function
    | Controller.Keyboard -> true
    | Controller.Joystick (index, _, guid) ->
      let info = Engine.Joystick.GetJoystickInfo(index)
      info <> null && info.GUID = guid

  let mutable dispachableIntervalTime = 0.0f

  override this.OnAdded() =
    let handler = {
      dispatch = updater.Dispatch
      handleGameControlEffect = function
        | GameControlEffect.Pause -> Engine.Pause(this)
        | GameControlEffect.Resume -> Engine.Resume()
        | GameControlEffect.Start(gameMode, controller) ->
          gameNode |> function
          | ValueSome n ->
            Engine.AddNode(n)
            n.Initialize(gameMode, controller)
          | ValueNone ->
            let uiRootGameInfo = BoxUIRootNode(isAutoTerminated = false)
            let gameInfoElement, gameInfoScoreUpdater, gameInfoTimeUpdater =
              GameInfoElement.createSologameInfoElement container

            uiRootGameInfo.SetElement gameInfoElement
            // let gameInfo = GameInfoNode(Position = Helper.SoloGame.gameInfoCenterPos)
            let gameHandler = { new IGameHandler with
              member __.SetModel(model) =
                BoxUISystem.Post (fun () ->
                  gameInfoScoreUpdater model
                )
              
              member __.SetTime(second) =
                BoxUISystem.Post (fun () ->
                  gameInfoTimeUpdater second
                )

              member __.FinishGame(model, t) =
                MenuV2.Msg.FinishGame(model, t)
                |> updater.Dispatch

                dispachableIntervalTime <- 1.0f

              member __.SelectController() =
                MenuV2.Msg.SelectController
                |> updater.Dispatch
            }
            let n = Game(gameHandler)
            n.Initialize(gameMode, controller)
            n.AddChildNode uiRootGameInfo
            Engine.AddNode(n)

            gameNode <- ValueSome n

        | GameControlEffect.Quit ->
          gameNode |> ValueOption.iter Engine.RemoveNode

        | GameControlEffect.Restart ->
          gameNode |> ValueOption.iter (fun n -> n.Restart())

      handleSetController = fun controller ->
        if isAvailableController controller then
          gameNode.Value.Controller <- ValueSome controller
          true
        else
          false
    }

    // let config = Config.tryGetConfig().Value

    updater.Init(MenuV2.State.Init (config), fun msg state ->
      let newState = MenuV2.update msg state |> Eff.handle handler
      Utils.DebugLogn (sprintf "Msg: %A\nState: %A\n" msg newState)
      newState
    )

    lastState <- updater.Model

  override this.OnUpdate() =
    if dispachableIntervalTime <= 0.0f then
      updater.Model.Value |> function
      | MenuV2.GameState (_, controller, _) ->
        if InputControl.pauseInput controller then
          updater.Dispatch MenuV2.Msg.PauseGame

      | MenuV2.SettingMenuState(SubMenu.Setting.State.InputName _, _)
      | MenuV2.GameResultState(SubMenu.GameResult.State.InputName _, _) ->
        InputControl.MenuV2.getCharacterInput ()
        |> Option.orElseWith getJoysticksInputs
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
    
    else
      dispachableIntervalTime <- dispachableIntervalTime - Engine.DeltaSecond
