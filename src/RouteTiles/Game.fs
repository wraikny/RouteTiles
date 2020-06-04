namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Effect
open RouteTiles.Core.Utils

open System
open Affogato
open Altseed
open EffFs

type Handler = {
  rand: Random
} with
  static member Handle(x) = x

  static member Handle(RandomEffect (Random.Generator f), k) =
    Eff.capture(fun h ->
      f h.rand |> k
    )


type Game() =
  inherit Node()

  let handler = { rand = Random(0) }

  let updater = Updater<Model.Game, _>()

  let coroutineNode = CoroutineNode()

  let board = Board()

  do
    updater
    |> Observable.map(fun x -> x.board)
    |> fun o -> o.Subscribe(board)
    |> ignore

  do
    let laneSlideKeys = [|
      Keys.Z, 0
      Keys.X, 1
      Keys.C, 2
      Keys.V, 3
    |]

    let mutable enabledSlideInput = true

    coroutineNode.Add(Coroutine.loop <| seq {
      if enabledSlideInput then
        // Slide
        let input =
          laneSlideKeys
          |> Seq.tryFind(fst >> Engine.Keyboard.GetKeyState >> (=) ButtonState.Push)

        match input with
        | None -> yield ()
        | Some (_, lane) ->
          updater.Dispatch(Update.GameMsg.SlideLane lane)
          enabledSlideInput <- false
          yield! Coroutine.sleep Consts.tileSlideInterval
          enabledSlideInput <- true
          yield()
    })

  override this.OnAdded() =
    this.AddChildNode(coroutineNode)
    this.AddChildNode(board)

    let gameModel =
      Model.Game.init Consts.nextsCount Consts.boardSize
      |> Eff.handle handler

    let update msg model =
#if DEBUG
      printfn "Msg: %A" msg
#endif
      Update.Game.update msg model
      |> Eff.handle handler

    updater.Init(gameModel, update)
