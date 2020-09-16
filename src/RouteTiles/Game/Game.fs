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
  emitVanishmentParticle: Set<Board.RouteOrLoop> -> unit
} with
  static member Handle(x) = x

  static member Handle(Random.RandomEffect f, k) =
    Eff.capture(fun h -> f h.rand |> k)

  static member Handle(Log.LogEffect s, k) =
    Engine.Log.Warn(LogCategory.User, s)
    k ()

  static member Handle(EmitVanishParticleEffect particleSet, k) =
    Eff.capture(fun h ->
      h.emitVanishmentParticle particleSet
      k()
    )

type IGameHandler =
  abstract SetModel: SoloGame.Model -> unit
  abstract SetTime: float32 -> unit
  abstract FinishGame: SoloGame.Model * time:float32 -> unit
  abstract SelectController: unit -> unit

type Game(gameMode, controller, gameInfoViewer: IGameHandler) =
  inherit Node()

  let mutable controller = controller

  let mutable lastModel: SoloGame.Model voption = ValueNone
  let updater = Updater<SoloGame.Model, _>()

  let coroutineNode = CoroutineNode()
  let boardNode = BoardNode(coroutineNode.Add, Position = Helper.SoloGame.boardViewPos)
  let nextTilesNode = NextTilesNode(coroutineNode.Add, Position = Helper.SoloGame.nextsViewPos)
  // let gameInfoNode = GameInfoNode(Helper.SoloGame.gameInfoCenterPos)

  let mutable inputEnabled = true

  let mutable time = 0.0f

  let initTime() =
    time <- gameMode |> function
      | SoloGame.Mode.ScoreAttack sec ->
        float32 sec
      | SoloGame.Mode.TimeAttack _ ->
        0.0f

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
      | SoloGame.Mode.TimeAttack score ->
        if model.board.point > score then
          // 終了
          coroutineNode.Add(seq {
            inputEnabled <- false
            yield! Coroutine.sleep Consts.Board.tilesVanishInterval
            gameInfoViewer.FinishGame(model, time)
          })
          ()
      | _ -> ()

      lastModel <- ValueSome model
    )
    |> ignore

    (gameMode |> function
    | SoloGame.Mode.ScoreAttack _ ->
      seq {
        while true do
          time <- time - Engine.DeltaSecond
          if time < 0.0f then
            // 終了
            inputEnabled <- false
            yield! Coroutine.sleep Consts.Board.tilesVanishInterval
            gameInfoViewer.FinishGame(lastModel.Value, 0.0f)
            gameInfoViewer.SetTime(0.0f)
          else
            gameInfoViewer.SetTime(time)
          yield()
      }
    | SoloGame.Mode.TimeAttack _ ->
      seq {
        while true do
          time <- time + Engine.DeltaSecond
          gameInfoViewer.SetTime(time)
          yield()
      }
    ) |> coroutineNode.Add

  /// Binding Input
  do
    coroutineNode.Add(seq {
      while true do

#if DEBUG
        if Engine.Keyboard.IsPushState Key.Num0 then
          printfn "%A" lastModel
#endif

        if inputEnabled then
          let input =
            controller |> function
            | Controller.Keyboard ->
              InputControl.SoloGame.getKeyboardInput()
            | Controller.Joystick (index, name, guid) ->
              let info = Engine.Joystick.GetJoystickInfo(index)
              if info <> null && info.GUID = guid then
                InputControl.SoloGame.getJoystickInput index
              else
                let s = sprintf "joystick '%s' is not found at %d" name index
                Engine.Log.Warn(LogCategory.User, s)
                // todo:コントローラー選択画面
                gameInfoViewer.SelectController()
                None

          match input with
          | None -> ()

          | Some (SoloGame.Msg.Board _ as msg) ->
            updater.Dispatch(msg)
            let m = updater.Model.Value

            yield! Coroutine.sleep Consts.GameCommon.inputInterval

            if not m.board.routesAndLoops.IsEmpty then
              yield! Coroutine.sleep Consts.Board.tilesVanishInterval
              updater.Dispatch(lift Board.Msg.ApplyVanishment) |> ignore

          // | Some msg ->
          //   updater.Dispatch(msg) |> ignore

          yield()
    })

  let handler: Handler = {
#if DEBUG
      rand = Random(0)
#else
      rand = Random()
#endif
      emitVanishmentParticle = boardNode.EmitVanishmentParticle
    }

  let initModel =
    let config: Board.BoardConfig = {
      nextCounts = Consts.Core.nextsCount
      size = Consts.Core.boardSize
    }

    SoloGame.init
      config
      gameMode
      // controller
    |> Eff.handle handler

  member val Controller = controller with get, set

  override this.OnAdded() =
    this.Initialize()

  member __.Initialize() =
    updater.Init(initModel, fun msg model ->
      Utils.DebugLogn (sprintf "Msg: %A" msg)
      SoloGame.update msg model
      |> Eff.handle handler
    )

    lastModel <- updater.Model

    initTime()
    inputEnabled <- true
