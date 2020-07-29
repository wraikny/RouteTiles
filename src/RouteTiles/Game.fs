namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects

open System
open Affogato
open Altseed2
open EffFs

type Handler = {
  rand: Random
  setIsPaused: bool -> unit
  emitVanishmentParticle: Set<Board.RouteOrLoop> -> unit
} with
  static member Handle(x) = x

  static member Handle(RandomEffect (Random.Generator f), k) =
    Eff.capture(fun h -> f h.rand |> k)

  static member Handle(LogEffect s, k) =
    Engine.Log.Warn(LogCategory.User, s)
    k ()

  static member Handle(effect, k) =
    Eff.capture(fun h ->
      match effect with
      | ControlEffect.SetIsPaused t -> h.setIsPaused t
      | ControlEffect.Restart -> ()
      | ControlEffect.Quit -> ()

      k()
    )

  static member Handle(EmitVanishParticleEffect particleSet, k) =
    Eff.capture(fun h ->
      h.emitVanishmentParticle particleSet
      k()
    )

type Game(gameMode, controller) =
  inherit Node()

  let mutable lastModel: SoloGame.Model voption = ValueNone
  let updater = Updater<SoloGame.Model, _>()

  let coroutineNode = CoroutineNode()
  let pauseNode = PauseNode(lift >> updater.Dispatch >> ignore)
  let boardNode = BoardNode(Helper.SoloGame.boardViewPos, coroutineNode.Add)
  let nextTilesNode = NextTilesNode(Helper.SoloGame.nextsViewPos, coroutineNode.Add)
  let gameInfoNode = GameInfoNode(Helper.SoloGame.gameInfoCenterPos)

  let initialize() =
    let handler: Handler = {
#if DEBUG
      rand = Random(0)
#else
      rand = Random()
#endif
      setIsPaused = fun t ->
        coroutineNode.IsUpdated <- not t

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
        controller
      
      |> Eff.handle handler

    let update msg model =
#if DEBUG
      printfn "Msg: %A" msg
#endif
      SoloGame.update msg model
      |> Eff.handle handler

    updater.Init(initModel, update) |> ignore


  /// Binding Children
  do
    base.AddChildNode(coroutineNode)

    base.AddChildNode(boardNode)
    base.AddChildNode(nextTilesNode)
    base.AddChildNode(gameInfoNode)

    base.AddChildNode(pauseNode)

    updater :> IObservable<_>
    |> Observable.subscribe(fun model ->
      if model.pause = Pause.Model.NotPaused then
        boardNode.OnNext(model.board)
        nextTilesNode.OnNext(model.board)
        gameInfoNode.OnNext(model.board)
      
      pauseNode.OnNext(model)
    )
    |> ignore

    updater :> IObservable<_>
    |> Observable.subscribe(fun model ->
      match lastModel with
      | ValueNone -> ()
      | ValueSome m ->
        match Pause.isPauseActivated m.pause model.pause with
        | ValueSome t -> coroutineNode.IsUpdated <- not t
        | _ -> ()

      lastModel <- ValueSome model
    )
    |> ignore

    let mutable time = 0.0f
    coroutineNode.Add(seq {
      while true do
        time <- time + Engine.DeltaSecond
        gameInfoNode.SetTime(time)
        yield()
    })

  /// Binding Input
  do
    coroutineNode.Add(seq {
      while true do
#if DEBUG
        if Engine.Keyboard.IsPushState Key.Num0 then
          printfn "%A" lastModel
#endif

        match lastModel with
        | ValueNone -> yield()
        | ValueSome model ->
          let input =
            model.controller |> function
            | Controller.Keyboard ->
              InputControl.SoloGame.getKeyboardInput()
            | Controller.Joystick (_, ValidJoystickIndex index) ->
              InputControl.SoloGame.getJoystickInput index
            | Controller.Joystick (name, index) ->
              let s = sprintf "joystick '%s' is not present at %d" name index
              Engine.Log.Warn(LogCategory.User, s)
              None

          match input with
          | None -> ()

          | Some (SoloGame.Msg.Board _ as msg) ->
            let m = updater.Dispatch(msg) |> ValueOption.get

            yield! Coroutine.sleep Consts.GameCommon.inputInterval

            if not m.board.routesAndLoops.IsEmpty then
              yield! Coroutine.sleep Consts.Board.tilesVanishInterval
              updater.Dispatch(lift Board.Msg.ApplyVanishment) |> ignore

          | Some msg ->
            updater.Dispatch(msg) |> ignore

          yield()
    })

  override __.OnAdded() = initialize()