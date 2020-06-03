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

  let board = Board()

  do
    updater
    |> Observable.map(fun x -> x.board)
    |> fun o -> o.Subscribe(board)
    |> ignore

  override this.OnAdded() =
    let laneSlideKeys = [|
      Keys.Z, 0
      Keys.X, 1
      Keys.C, 2
      Keys.V, 3
    |]

    let mutable enabledSlideInput = true

    this.AddChildNode({ new Node() with
        member __.OnUpdate() =
          if enabledSlideInput then
            // Slide
            laneSlideKeys
            |> Seq.tryFind(fun (key, _) -> Engine.Keyboard.GetKeyState(key) = ButtonState.Push)
            |> Option.iter(fun (_, lane) ->
              updater.Dispatch(Update.GameMsg.SlideLane lane)
              async {
                enabledSlideInput <- false
                do! Async.Sleep (int Consts.tileSlideInterval)
                enabledSlideInput <- true
              } |> Async.StartImmediate
            )
    })

    this.AddChildNode(board)

    let gameModel =
      Model.Game.init Consts.nextsCount Consts.boardSize
      |> Eff.handle handler


    let update msg model =
      Update.Game.update msg model
      |> Eff.handle handler

    updater.Init(gameModel, update)
