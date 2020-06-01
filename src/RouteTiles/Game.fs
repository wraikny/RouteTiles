namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Effect
open RouteTiles.Core.Utils

open System
open Affogato
open Altseed
open EffFs
open Elmish.Reactive

type Handler = {
  rand: Random
} with
  static member Handle(x) = x

  static member Handle(RandomInt(min, max), k) =
    Eff.capture(fun h ->
      h.rand.Next(min, max) |> k
    )

  static member Handle(RandomIntArray(length, (min, max)), k) =
    Eff.capture(fun h ->
      [| for _ in 1..length -> h.rand.Next(min, max) |] |> k
    )


type Game() =
  inherit Node()

  let handler = { rand = Random(0) }

  let program =
    let gameModel =
      Model.Game.init Consts.nextsCount Consts.boardSize
      |> Eff.handle handler

    RxProgram.mkSimple
      gameModel
      (fun msg model ->
        Update.Game.update msg model
        |> Eff.handle handler
      )

  let board = Board()

  do
    program
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
              program.Dispatch(Update.GameMsg.SlideLane lane)
              async {
                enabledSlideInput <- false
                do! Async.Sleep (int Consts.tileSlideInterval)
                enabledSlideInput <- true
              } |> Async.StartImmediate
            )
    })

    this.AddChildNode(board)

    program.Run()
