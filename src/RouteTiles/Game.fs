namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Effects

open System
open Affogato
open Altseed2
open EffFs

type Handler = {
  rand: Random
} with
  static member Handle(x) = x

  static member Handle(RandomEffect (Random.Generator f), k) =
    Eff.capture(fun h ->
      let res = f h.rand

#if DEBUG
      printfn "RandomEffect(%A)" res
#endif

      k res
    )

type Game(gameMode, controller) =
  inherit Node()

  let updater = Updater<SoloGame.Model, _>()

  let coroutineNode = CoroutineNode()

  /// Binding Children
  do
    base.AddChildNode(coroutineNode)

    let board = BoardNode(Helper.boardViewPos, coroutineNode.Add)
    base.AddChildNode(board)

    let gameInfo = GameInfoNode(Helper.gameInfoCenterPos)
    base.AddChildNode(gameInfo)

    updater :> IObservable<_>
    |> Observable.subscribe(fun model ->
      board.OnNext(model.board)
      gameInfo.OnNext(model.board)
    )
    |> ignore

    let mutable time = 0.0f
    coroutineNode.Add(seq {
      while true do
        time <- time + Engine.DeltaSecond
        gameInfo.SetTime(time)
        yield()
    })

  /// Binding Input
  do
    coroutineNode.Add(seq {
      while true do
#if DEBUG
        if Engine.Keyboard.IsPushState Keys.Num0 then
          printfn "%A" updater.Model
#endif

        match updater.Model with
        | ValueNone -> yield()
        | ValueSome model ->
          let input = model.controller |> function
            | Controller.Keyboard ->
              InputControl.Board.getKeyboardInput()
            | Controller.Joystick (_, index) when Engine.Joystick.IsPresent index ->
              InputControl.Board.getJoystickInput index
            | Controller.Joystick (_, _) ->
              // start ControllerSelect
              // coroutineNode.IsUpdated <- false
              None

          match input with
          | None ->
            yield ()

          | Some msg ->
            let m = updater.Dispatch(lift msg) |> ValueOption.get

            yield! Coroutine.sleep Consts.inputInterval

            if not m.board.routesAndLoops.IsEmpty then
              yield! Coroutine.sleep Consts.tilesVanishInterval
              updater.Dispatch(lift Board.Msg.ApplyVanishment) |> ignore
              yield()
    })

  override __.OnAdded() =
    let handler: Handler = {
#if DEBUG
      rand = Random(0)
#else
      rand = Random()
#endif
    }

    let initModel =
      let config: Board.Model.BoardConfig = {
        nextCounts = Consts.nextsCount
        size = Consts.boardSize
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

