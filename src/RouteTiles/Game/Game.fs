namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects

open System
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

  let mutable lastModel: SoloGame.Model voption = ValueNone
  let updater = Updater<SoloGame.Model, _>()

  let coroutineNode = CoroutineNode()
  let boardNode = BoardNode(coroutineNode.Add, Position = Helper.SoloGame.boardViewPos)
  let nextTilesNode = NextTilesNode(coroutineNode.Add, Position = Helper.SoloGame.nextsViewPos)
  // let gameInfoNode = GameInfoNode(Helper.SoloGame.gameInfoCenterPos)

  let mutable inputEnabled = true

  let mutable time = 0.0f

  /// Binding Children
  do
    base.AddChildNode(coroutineNode)

    base.AddChildNode(boardNode)
    base.AddChildNode(nextTilesNode)
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
      gameMode |> function
      | ValueSome(SoloGame.Mode.TimeAttack score) ->
        if model.board.point > score then
          // 終了
          coroutineNode.Add(seq {
            inputEnabled <- false
            yield! Coroutine.sleep Consts.GameCommon.inputInterval
            yield! Coroutine.sleep Consts.Board.tilesVanishInterval
            gameInfoViewer.FinishGame(model, time)
          })
      | _ -> ()

      lastModel <- ValueSome model
    )
    |> ignore

    
    seq {
      while true do
        match gameMode with
        | ValueSome(SoloGame.Mode.ScoreAttack _) ->
          time <- time - Engine.DeltaSecond
          if time < 0.0f then
            // 終了
            inputEnabled <- false
            yield! Coroutine.sleep Consts.GameCommon.inputInterval
            yield! Coroutine.sleep Consts.Board.tilesVanishInterval
            gameInfoViewer.FinishGame(lastModel.Value, 0.0f)
            gameInfoViewer.SetTime(0.0f)
          else
            gameInfoViewer.SetTime(time)
        
        | ValueSome(SoloGame.Mode.TimeAttack _) ->
          time <- time + Engine.DeltaSecond
          gameInfoViewer.SetTime(time)
        | ValueNone ->
          ()
        yield()
      } |> coroutineNode.Add

  /// Binding Input
  do
    let invokeInput msg = seq {
      match msg with
      | Some(msg) ->
        match msg with
        | SoloGame.Msg.Board(Board.Msg.Slide _) ->
          soundControl.PlaySE(SEKind.GameMoveTiles)
        | SoloGame.Msg.Board(Board.Msg.MoveCursor _) ->
          soundControl.PlaySE(SEKind.GameMoveCursor)
        | _ -> ()

        updater.Dispatch(msg)
        let m = updater.Model.Value

        yield! Coroutine.sleep Consts.GameCommon.inputInterval

        if not m.board.routesAndLoops.IsEmpty then
        // todo: SoundEffect
          yield! Coroutine.sleep Consts.Board.tilesVanishInterval
          soundControl.PlaySE(SEKind.GameVanishTiles)
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
              // todo:コントローラー選択画面
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

  member __.Controller with get() = controller and set(v) = controller <- v

  member __.Initialize(gameMode_, controller_) =
    let initModel =
      let config: Board.BoardConfig = {
        nextCounts = Consts.Core.nextsCount
        size = Consts.Core.boardSize
      }

      SoloGame.init
        config
        gameMode_
        // controller
      |> Eff.handle handler

    updater.Init(initModel, fun msg model ->
      Utils.DebugLogn (sprintf "Msg: %A" msg)
      SoloGame.update msg model
      |> Eff.handle handler
    )

    lastModel <- updater.Model

    time <- gameMode_ |> function
      | SoloGame.Mode.ScoreAttack sec ->
        float32 sec
      | SoloGame.Mode.TimeAttack _ ->
        0.0f
    
    inputEnabled <- true

    gameMode <- ValueSome gameMode_
    controller <- ValueSome controller_

  member this.Restart() =
    (gameMode, controller) |> function
    | ValueSome gameMode, ValueSome controller ->
      this.Initialize(gameMode, controller)
    | _ -> failwith "invalid state"