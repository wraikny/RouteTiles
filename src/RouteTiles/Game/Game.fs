namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects

open System
open System.Collections.Generic
open Affogato
open Altseed2
open EffFs
open EffFs.Library

type Handler = {
  rand: Random
  // emitVanishmentParticle: Set<Board.RouteOrLoop> -> unit
} with
  static member Handle(x) = x

  static member Handle(Random.RandomEffect f, k) =
    Eff.capture(fun h -> f h.rand |> k)

  static member Handle(Log.LogEffect s, k) =
    Engine.Log.Warn(LogCategory.User, s)
    k ()

  // static member Handle(EmitVanishParticleEffect particleSet, k) =
  //   Eff.capture(fun h ->
  //     h.emitVanishmentParticle particleSet
  //     k()
  //   )

type internal IGameHandler =
  abstract SetModel: SoloGame.Model -> unit
  abstract SetTime: float32 -> unit
  abstract FinishGame: SoloGame.Model * time:float32 -> unit
  abstract SelectController: unit -> unit

type internal Game(gameInfoViewer: IGameHandler, soundControl: SoundControl) =
  inherit Node()

  let mutable gameMode = ValueNone
  let mutable controller = ValueNone
  let mutable config = ValueNone

  let mutable lastModel: SoloGame.Model voption = ValueNone
  let updater = Updater<SoloGame.Model, _>()

  let coroutineNode = CoroutineNode()
  let childrenCoroutineNode = CoroutineNode()
  let boardNode = BoardNode(childrenCoroutineNode.Add, Position = Helper.SoloGame.boardViewPos)
  let nextTilesNode = NextTilesNode(childrenCoroutineNode.Add, Position = Helper.SoloGame.nextsViewPos)
  // let gameInfoNode = GameInfoNode(Helper.SoloGame.gameInfoCenterPos)
  let backgroundPostEffect = PostEffect.PostEffectBackground(ZOrder = ZOrder.posteffect)

  let readyStartNode =
    ReadyStart
      ( soundControl
      , Helper.SoloGame.boardViewPos + 0.5f * Helper.Board.boardViewSize)

  let mutable inputEnabled = true

  /// Binding Children
  do
    base.AddChildNode(coroutineNode)
    base.AddChildNode(childrenCoroutineNode)

    base.AddChildNode(boardNode)
    base.AddChildNode(nextTilesNode)

    base.AddChildNode(readyStartNode)

    base.AddChildNode(backgroundPostEffect)
    // base.AddChildNode(gameInfoNode)

    updater :> IObservable<_>
    |> Observable.subscribe(fun model ->
      boardNode.OnNext(model.board)
      nextTilesNode.OnNext(model.board)
      gameInfoViewer.SetModel(model)
    )
    |> ignore

    updater :> IObservable<_>
    |> Observable.subscribe(fun model ->
      lastModel <- ValueSome model
    )
    |> ignore

  /// Binding Input
  do
    let invokeInput msg = seq {
      match msg with
      | Some(msg) ->
        match msg with
        | SoloGame.Msg.Board(Board.Msg.Slide _) ->
          soundControl.PlaySE(SEKind.GameMoveTiles, true)
        | SoloGame.Msg.Board(Board.Msg.MoveCursor _) ->
          soundControl.PlaySE(SEKind.GameMoveCursor, true)
        | _ -> ()

        updater.Dispatch(msg)
        let m = updater.Model.Value

        yield! Coroutine.sleep Consts.GameCommon.inputInterval

        if not m.board.routesAndLoops.IsEmpty then
          yield! Coroutine.sleep Consts.Board.tilesVanishInterval
          soundControl.PlaySE(SEKind.GameVanishTiles, true)
          boardNode.EmitVanishmentParticle(m.board.routesAndLoops)
          updater.Dispatch(lift Board.Msg.ApplyVanishment) |> ignore
      | _ -> yield ()
    }

    coroutineNode.Add(seq {
      while true do
#if DEBUG
        if Engine.Keyboard.IsPushState Key.Num0 then
          printfn "%A" lastModel
#endif

        if inputEnabled then
          match controller with
          | ValueSome Controller.Keyboard ->
            let msg = InputControl.SoloGame.getKeyboardInput()
            yield! invokeInput msg
          | ValueSome (Controller.Joystick (index, name, guid)) ->
            let info = Engine.Joystick.GetJoystickInfo(index)
            if info <> null && info.GUID = guid then
              let msg = InputControl.SoloGame.getJoystickInput index
              yield! invokeInput msg
            else
              let s = sprintf "joystick '%s' is not found at %d" name index
              Engine.Log.Warn(LogCategory.User, s)
              gameInfoViewer.SelectController()
              yield ()
          | ValueNone ->
            yield ()
        else
          yield ()
    })

  let handler: Handler = {
#if DEBUG
      rand = Random(0)
#else
      rand = Random()
#endif
      // emitVanishmentParticle = 
    }

  let mutable uniqueCoroutine:IEnumerator<unit> = null
  let mutable disposable: IDisposable = null

  override __.OnUpdate() =
    if uniqueCoroutine <> null then
      if not <| uniqueCoroutine.MoveNext() then
        uniqueCoroutine <- null

  member this.Clear() =
    boardNode.Clear()
    nextTilesNode.Clear()
    childrenCoroutineNode.Clear()
    this.FlushQueue()

  member __.Controller with get() = controller and set(v) = controller <- v

  member __.Initialize'() =
    // lastModel <- updater.Model
    inputEnabled <- true

    let startTime = Engine.Time

    gameMode.Value |> function
    | SoloGame.Mode.Endless ->
      uniqueCoroutine <- (seq {
        while true do
          gameInfoViewer.SetTime(Engine.Time - startTime)
          yield ()
      }).GetEnumerator()

    | SoloGame.Mode.ScoreAttack time ->
      let time = float32 time
      uniqueCoroutine <- (seq {
        while Engine.Time - startTime < time do
          gameInfoViewer.SetTime(time - (Engine.Time - startTime))
          yield ()

        gameInfoViewer.SetTime(0.0f)
        inputEnabled <- false

        yield! Coroutine.sleep Consts.GameCommon.inputInterval
        yield! Coroutine.sleep Consts.Board.tilesVanishInterval

        gameInfoViewer.FinishGame(lastModel.Value, 0.0f)

        ()
      }).GetEnumerator()

    | SoloGame.Mode.TimeAttack score ->
      let mutable finished = false
      disposable <- updater.Subscribe(fun model ->
        if model.board.point > score then
          finished <- true

          if disposable <> null then
            disposable.Dispose()
            disposable <- null

          coroutineNode.Add(seq {
            inputEnabled <- false
            yield! Coroutine.sleep Consts.GameCommon.inputInterval
            yield! Coroutine.sleep Consts.Board.tilesVanishInterval
            gameInfoViewer.FinishGame(model, time)
          })
      )
      uniqueCoroutine <- (seq {
        while not finished do
          gameInfoViewer.SetTime(Engine.Time - startTime)
          yield ()
      }).GetEnumerator()

  member this.Initialize(gameMode_, controller_, config_: Config) =
    uniqueCoroutine <- null

    gameMode <- ValueSome gameMode_
    controller <- ValueSome controller_
    config <- ValueSome config_

    backgroundPostEffect.SetShader(config_.background |> Helper.PostEffect.toPath)

    if disposable <> null then
      disposable.Dispose()
      disposable <- null

    let initModel =
      let boardConfig: Board.BoardConfig = {
        nextCounts = Consts.Core.nextsCount
        size = Consts.Core.boardSize
      }

      SoloGame.init
        boardConfig
        gameMode_
        // controller
      |> Eff.handle handler

    updater.Init(initModel, fun msg model ->
      let newModel = SoloGame.update msg model |> Eff.handle handler
      Utils.DebugLogn (sprintf "Msg: %A" msg)
      Utils.DebugLogn (sprintf "Model: %A" newModel)
      newModel
    )

    let initTime = gameMode_ |> function
      | SoloGame.Mode.ScoreAttack time -> float32 time
      | SoloGame.Mode.TimeAttack _
      | SoloGame.Mode.Endless -> 0.0f

    gameInfoViewer.SetModel(initModel)
    gameInfoViewer.SetTime(initTime)

    inputEnabled <- false

    readyStartNode.Ready (fun () ->
      this.Initialize'()
    )

  member this.Restart() =
    (gameMode, controller, config) |> function
    | ValueSome gameMode, ValueSome controller, ValueSome config ->
      this.Initialize(gameMode, controller, config)
    | _ -> failwith "invalid state"
