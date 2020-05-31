namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Effect

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
      Model.Game.init 3 (Vector2.init 4 5)
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
    let keys = [|
      Keys.Z, 0
      Keys.X, 1
      Keys.C, 2
      Keys.V, 3
    |]

    this.AddChildNode({ new Node() with
        member __.OnUpdate() =
          for (key, lane) in keys do
            if Engine.Keyboard.GetKeyState(key) = ButtonState.Push then
              program.Dispatch(Update.GameMsg.SlideLane lane)
    })

    this.AddChildNode(board)

    program.Run()
