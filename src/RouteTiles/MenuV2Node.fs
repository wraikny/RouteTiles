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

  let toTable = function
    | SoloGame.GameMode.TimeAttack5000 -> Server.tableTime5000
    | SoloGame.GameMode.ScoreAttack180 -> Server.tableScore180
    | _ -> failwith "Unexpected"

  let select gameMode: Async<_> =
    let orderKey, isDescending = gameMode |> function
      | SoloGame.GameMode.TimeAttack5000 -> "Time", false
      | SoloGame.GameMode.ScoreAttack180 -> "Point", true
      | _ -> failwith "Unexpected"

    let table = toTable gameMode

    async {
      let! res =
        client.AsyncSelect<RouteTiles.Core.Types.Ranking.Data>
          ( table
          , orderBy = orderKey
          , isDescending = isDescending
          , limit = 1000
          )

      Utils.DebugLogn (sprintf "select from %s: %A" table res)

      return res
    }

  let insertSelect guid gameMode (data: RouteTiles.Core.Types.Ranking.Data) =
    let table = toTable gameMode

    async {
      let! id = client.AsyncInsert(table, guid, data)

      Utils.DebugLogn (sprintf "insert '%A' to %s: %A" data table id)

      let! data = select gameMode

      return (id, data)
    }


module internal MenuUtil =
  let getCurrentControllers() =
    [|
      yield Controller.KeyboardShift
      yield Controller.KeyboardSeparate
      for i in 0..15 do
        let info = Engine.Joystick.GetJoystickInfo i
        if info <> null && info.IsGamepad then
          yield Controller.Joystick(i, info.GamepadName, info.GUID)
    |]


type MenuV2Handler = {
  dispatch: MenuV2.Msg -> unit
  handleGameControlEffect: GameControlEffect -> unit
  handleSetController: Controller -> bool
  soundControl: SoundControl
} with
  static member Handle(x) = x

  static member inline Handle(e, k) = Library.StateMachine.handle (e, k)

  static member Handle(e: SoundEffect, k) =
    Utils.DebugLogn (sprintf "SoundEffect: %A" e)
    Eff.capture(fun h ->
      e |> function
        | SoundEffect.Select -> SEKind.Enter
        | SoundEffect.Move -> SEKind.CursorMove
        | SoundEffect.Cancel -> SEKind.Cancel
        | SoundEffect.Invalid -> SEKind.Invalid
        | SoundEffect.InputChar -> SEKind.InputChar
        | SoundEffect.DeleteChar -> SEKind.Invalid
      |> fun k -> h.soundControl.PlaySE (k, false)
      k ()
    )

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
    Eff.capture (fun h ->
      Utils.DebugLogn (sprintf "Effect: %A" e)
      h.soundControl.SetVolume(config.bgmVolume, config.seVolume)
      Config.save config
      k()
    )

  static member Handle(ErrorLogEffect error as e, k) =
    Utils.DebugLogn (sprintf "Effect: %A" e)
    ErrorLog.save error
    k ()

  static member Handle(SetSoundVolume(bgmVolume, seVolume) as e, k) =
    Eff.capture(fun h ->
      Utils.DebugLogn (sprintf "Effect: %A" e)
      h.soundControl.SetVolume(bgmVolume, seVolume)
      k()
    )

  static member Handle(e: GameRankingEffect, k) =
    Eff.capture(fun h ->
      async {
        match e with
        | GameRankingEffect.InsertSelect (guid, gameMode, data) ->
          let! res = RankingServer.insertSelect guid gameMode data  |> Async.Catch
          let res = res |> function
            | Choice1Of2 (id, data) -> Ok (id, data)
            | Choice2Of2 e -> Error e
          h.dispatch <| MenuV2.Msg.ReceiveRankingGameResult res

        | GameRankingEffect.SelectAll ->
          let! res =
            RankingServer.select SoloGame.GameMode.TimeAttack5000
            |> Async.CatchResult
            |> AsyncResult.bind (fun time5000 -> async {
              let! resScore180 =
                RankingServer.select SoloGame.GameMode.ScoreAttack180
                |> Async.CatchResult
              
              return
                resScore180
                |> Result.map(fun score180 ->
                  Map.ofArray [|
                    SoloGame.GameMode.TimeAttack5000, time5000
                    SoloGame.GameMode.ScoreAttack180, score180
                  |]
                )
            })
          h.dispatch <| MenuV2.Msg.ReceiveRankingRankings res
      }
      |> Async.StartImmediate
      k ()
    )


type internal MenuV2Node(config: Config, container: Container) =
  inherit Node()

  let mutable lastState = ValueNone
  let updater = Updater<MenuV2.State, MenuV2.Msg>()

  let coroutineNode = CoroutineNode()

  let uiRootMenu = BoxUIRootNode()
  let uiRootModal = BoxUIRootNode()

  let soundControl = SoundControl(config.bgmVolume, config.seVolume)

  let lightBloom =
    PostEffectLightBloomNode
      ( Intensity = Consts.ViewCommon.LightBloomIntensity
      , Exposure = Consts.ViewCommon.LightBloomExposure
      , Threshold = Consts.ViewCommon.LightBloomThreshold
      , ZOrder = ZOrder.PostEffect.lightBloom
      )

  let postEffectFade = PostEffect.PostEffectFade(ZOrder = ZOrder.PostEffect.fade)

  let mutable gameNode: Game voption = ValueNone

  do
    base.AddChildNode coroutineNode
    base.AddChildNode uiRootMenu
    base.AddChildNode uiRootModal
    base.AddChildNode soundControl
    base.AddChildNode lightBloom

    soundControl.SetState(SoundControlState.Menu)

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
    | Controller.KeyboardShift
    | Controller.KeyboardSeparate -> true
    | Controller.Joystick (index, _, guid) ->
      let info = Engine.Joystick.GetJoystickInfo(index)
      info <> null && info.GUID = guid

  let mutable dispachableIntervalTime = 0.0f

  override this.OnAdded() =
    let initWithFading f =
      dispachableIntervalTime <- 1.0f
      Engine.AddNode postEffectFade

      coroutineNode.Add(seq {
        for t in Coroutine.milliseconds 500<millisec> do
          postEffectFade.FadeRate <- t
          yield ()

        postEffectFade.FadeRate <- 1.0f
        f()
        yield ()

        for t in Coroutine.milliseconds 500<millisec> do
          postEffectFade.FadeRate <- 1.0f - t
          yield ()


        Engine.RemoveNode postEffectFade
      })


    let handler = {
      dispatch = updater.Dispatch
      soundControl = soundControl

      handleGameControlEffect = function
        | GameControlEffect.Pause ->
          Engine.Pause(this)
          let config = Config.tryGet().Value
          soundControl.PauseSE()
          soundControl.SetVolume(config.bgmVolume * 0.25f, config.seVolume)
          soundControl.PlaySE(SEKind.Pause, false)

        | GameControlEffect.Resume ->
          Engine.Resume()
          let config = Config.tryGet().Value
          soundControl.SetVolume(config.bgmVolume, config.bgmVolume)
          soundControl.ResumeSE()

        | GameControlEffect.Start(gameMode, controller, config) ->
          soundControl.SetState(SoundControlState.Game)

          gameNode |> function
          | ValueSome n ->
            initWithFading (fun () ->
              n.Initialize(gameMode, controller, config)
              Engine.AddNode(n)
            )
          | ValueNone ->
            let gameHandler = { new IGameHandler with
              member __.FinishGame(model, t) =
                soundControl.StopSE()
                soundControl.SetState(SoundControlState.Menu)

                MenuV2.Msg.FinishGame(model, t)
                |> updater.Dispatch

                dispachableIntervalTime <- 0.5f

              member __.SelectController() =
                MenuV2.Msg.SelectController
                |> updater.Dispatch
            }
            let n = Game(container, gameHandler, soundControl)
            gameNode <- ValueSome n

            Engine.AddNode postEffectFade
            initWithFading (fun () ->
              Engine.AddNode(n)
              n.Initialize(gameMode, controller, config)
            )

        | GameControlEffect.Quit ->
          soundControl.StopSE()
          if soundControl.State <> ValueSome SoundControlState.Menu then
            soundControl.SetState(SoundControlState.Menu)

          gameNode.Value.Clear()
          // gameNode.Value.FlushQueue()
          async {
            do! Async.Sleep 1
            Engine.RemoveNode gameNode.Value
          }
          |> Async.StartImmediate

        | GameControlEffect.Restart ->
          soundControl.StopSE()
          soundControl.SetState(SoundControlState.Game)
          gameNode.Value.Clear()
          // gameNode.Value.FlushQueue()
          async {
            do! Async.Sleep 1
            gameNode.Value.Restart()
          }
          |> Async.StartImmediate

      handleSetController = fun controller ->
        if isAvailableController controller then
          gameNode.Value.Controller <- ValueSome controller
          true
        else
          false
    }

    updater.Init(MenuV2.State.Init (config), fun msg state ->
      let newState = MenuV2.update msg state |> Eff.handle handler
      Utils.DebugLogn (sprintf "Msg: %A\nState: %A\n" msg newState)
      newState
    )

    lastState <- updater.Model

    // 名前未設定の場合は初心者っぽい。
    if config.name.IsNone then
      updater.Dispatch MenuV2.Msg.OpenHowToControl


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
