namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects

open System
open System.Collections.Generic
open Affogato
open Altseed2
open Altseed2.BoxUI
open EffFs
open EffFs.Library

type Handler = {
  rand: Random
  // emitVanishmentParticle: Set<Board.RouteOrLoop> -> unit
} with
  static member Handle(x) = x

  static member Handle(Random.RandomEffect f, k) =
    Eff.capture(fun h -> f h.rand |> k)

  // static member Handle(Log.LogEffect s, k) =
  //   Engine.Log.Warn(LogCategory.User, s)
  //   k ()

type internal IGameHandler =
  // abstract SetModel: SoloGame.Model -> unit
  // abstract SetTime: float32 -> unit
  abstract FinishGame: SoloGame.Model * time:float32 -> unit
  abstract SelectController: unit -> unit

type internal Game(container: MenuV2.Container, gameInfoViewer: IGameHandler, soundControl: SoundControl) =
  inherit Node()

  let mutable gameMode = ValueNone
  let mutable controller = ValueNone
  let mutable config = ValueNone

  let updater = Updater<SoloGame.Model, _>()

  let coroutineNode = CoroutineNode()
  let childrenCoroutineNode = CoroutineNode()

  let boardNode = BoardNode(childrenCoroutineNode.Add, Position = Helper.SoloGame.boardViewPos)
  let scoreEffect = ScoreEffect(container.Font, Color(255, 255, 255, 255), childrenCoroutineNode.Add)

  let nextTilesNode = NextTilesNode(childrenCoroutineNode.Add, Position = Helper.SoloGame.nextsViewPos)
  let uiRootGameInfoNode = BoxUIRootNode(isAutoTerminated = false)

  let setModel, setTime =
    let gameInfoElement, gameInfoScoreUpdater, gameInfoTimeUpdater =
      MenuV2.GameInfoElement.createSologameInfoElement container

    uiRootGameInfoNode.SetElement gameInfoElement

    (fun model -> BoxUISystem.Post(fun () -> gameInfoScoreUpdater model)),
    (fun time -> BoxUISystem.Post(fun () -> gameInfoTimeUpdater time))

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
    boardNode.AddChildNode(scoreEffect)
    
    base.AddChildNode(nextTilesNode)
    base.AddChildNode(uiRootGameInfoNode)

    base.AddChildNode(readyStartNode)

    base.AddChildNode(backgroundPostEffect)

    updater :> IObservable<_>
    |> Observable.subscribe(fun model ->
      boardNode.OnNext(model.board)
      nextTilesNode.OnNext(model.board)
      
      setModel model
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

        match m.board.routesAndLoops with
        | ValueNone -> ()
        | ValueSome (routesAndLoops, newPoint) ->
          yield! Coroutine.sleep Consts.Board.tilesVanishInterval

          soundControl.PlaySE(SEKind.GameVanishTiles, true)
          boardNode.EmitVanishmentParticle(routesAndLoops)

          let scoreEffectPosition =
            let tileCenterPoses =
              [|
                for rl in routesAndLoops do
                  for (cdn, id) in rl.Value do
                    (cdn, id)
              |]
              |> Array.distinctBy snd
              |> Array.map(fun (cdn, _) ->
                let v = Helper.Board.calcTilePosCenter cdn
                Vector2.init v.X v.Y
              )
            
            (Array.sum tileCenterPoses) ./ (float32 tileCenterPoses.Length)
          scoreEffect.Add(newPoint, scoreEffectPosition |> fromAffogato)

          updater.Dispatch(lift Board.Msg.ApplyVanishment) |> ignore

      | _ -> yield ()
    }

    coroutineNode.Add(seq {
      while true do
        if inputEnabled then
          match controller with
          | ValueSome Controller.KeyboardShift ->
            let msg = InputControl.SoloGame.getKeyboardShiftInput()
            yield! invokeInput msg
          | ValueSome Controller.KeyboardSeparate ->
            let msg = InputControl.SoloGame.getKeyboardSeparateInput()
            yield! invokeInput msg

          | ValueSome (Controller.Joystick (index, name, guid)) ->
            let info = Engine.Joystick.GetJoystickInfo(index)
            if info <> null && info.GUID = guid then
              let msg = InputControl.SoloGame.getJoystickInput index
              yield! invokeInput msg
            else
              Utils.DebugLogn(sprintf "joystick '%s' is not found at %d" name index)
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
    }

  let mutable uniqueCoroutine:IEnumerator<unit> = null


  let finishGame time =
    seq {
      inputEnabled <- false
      yield! Coroutine.sleep Consts.GameCommon.inputInterval
      yield! Coroutine.sleep Consts.Board.tilesVanishInterval
      gameInfoViewer.FinishGame(updater.Model.Value, time)
    }

  override __.OnUpdate() =
    if uniqueCoroutine <> null then
      if not <| uniqueCoroutine.MoveNext() then
        uniqueCoroutine <- null

  member this.Clear() =
    boardNode.Clear()
    nextTilesNode.Clear()
    scoreEffect.Clear()
    childrenCoroutineNode.Clear()

  member __.Controller with get() = controller and set(v) = controller <- v

  member __.Initialize'() =
    inputEnabled <- true

    let startTime = Engine.Time

    gameMode.Value |> function
    | SoloGame.Mode.Endless ->
      uniqueCoroutine <- (seq {
        while true do
          setTime(Engine.Time - startTime)
          yield ()
      }).GetEnumerator()

    | SoloGame.Mode.ScoreAttack time ->
      uniqueCoroutine <- (seq {
        let time = float32 time
        while Engine.Time - startTime < time do
          setTime(time - (Engine.Time - startTime))
          yield ()

        setTime(0.0f)
        yield! finishGame 0.0f
      }).GetEnumerator()

    | SoloGame.Mode.TimeAttack score ->

      uniqueCoroutine <- (seq {
        while updater.Model.Value.board.point < score do
          setTime(Engine.Time - startTime)
          yield ()
        
        let resTime = Engine.Time - startTime
        setTime resTime
        yield! finishGame resTime
      }).GetEnumerator()

  member this.Initialize(gameMode_, controller_, config_: Config) =
    uniqueCoroutine <- null

    gameMode <- ValueSome gameMode_
    controller <- ValueSome controller_
    config <- ValueSome config_

    backgroundPostEffect.SetShader(config_.background |> Helper.PostEffect.toPath)

    // initialize GameModel
    let initModel =
      let boardConfig: Board.BoardConfig = {
        nextCounts = Consts.Core.nextsCount
        size = Consts.Core.boardSize
      }

      SoloGame.init boardConfig gameMode_
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

    setModel(initModel)
    setTime(initTime)

    inputEnabled <- false
    readyStartNode.Ready (fun () -> this.Initialize'())

  member this.Restart() =
    (gameMode, controller, config) |> function
    | ValueSome gameMode, ValueSome controller, ValueSome config ->
      this.Initialize(gameMode, controller, config)
    | _ -> failwith "invalid state"
