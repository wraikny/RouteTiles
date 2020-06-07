namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Effects

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
      let res = f h.rand

#if DEBUG
      printfn "RandomEffect(%A)" res
#endif

      k res
    )

type Game() =
  inherit Node()

  let updater = Updater<Board.Model.Board, _>()

  let coroutineNode = CoroutineNode()

  let viewBaseNode = Node()
  let observerUnregisterers = ResizeArray<_>()

  let registerViewNode(viewNode) =
    let observableUpdater = updater :> IObservable<_>
    observableUpdater.Subscribe(viewNode) |> observerUnregisterers.Add
    viewBaseNode.AddChildNode(viewNode)

  do
    BoardNode(Helper.boardViewPos)
    |> registerViewNode

  override this.OnAdded() =
    this.AddChildNode(coroutineNode)
    this.AddChildNode(viewBaseNode)

    this.BindingInput()

    let handler: Handler = {
#if DEBUG
      rand = Random(0)
#else
      rand = Random()
#endif
    }

    let initModel =
      Board.Model.Board.init {
        nextCounts = Consts.nextsCount
        size = Consts.boardSize
      }
      |> Eff.handle handler

    let update msg model =
#if DEBUG
      printfn "Msg: %A" msg
#endif
      Board.Update.update msg model
      |> Eff.handle handler

    updater.Init(initModel, update)

  member private __.BindingInput() =
    // let inputs = [|
    //   JoystickButtonType.LeftUp, Board.MoveCursor Dir.Up
    //   JoystickButtonType.LeftRight, Board.MoveCursor Dir.Right
    //   JoystickButtonType.LeftDown, Board.MoveCursor Dir.Down
    //   JoystickButtonType.LeftLeft, Board.MoveCursor Dir.Left
    //   JoystickButtonType.RightUp, Board.Slide Dir.Up
    //   JoystickButtonType.RightRight, Board.Slide Dir.Right
    //   JoystickButtonType.RightDown, Board.Slide Dir.Down
    //   JoystickButtonType.RightLeft, Board.Slide Dir.Left
    // |]

    let inputs = [|
      Keys.W, Board.MoveCursor Dir.Up
      Keys.D, Board.MoveCursor Dir.Right
      Keys.S, Board.MoveCursor Dir.Down
      Keys.A, Board.MoveCursor Dir.Left
      Keys.I, Board.Slide Dir.Up
      Keys.L, Board.Slide Dir.Right
      Keys.K, Board.Slide Dir.Down
      Keys.J, Board.Slide Dir.Left
    |]
  
    let mutable enabledSlideInput = true

    coroutineNode.Add(Coroutine.loop <| seq {
      if enabledSlideInput then
        // Slide
        let input =
          // inputs
          // |> Seq.tryFind(fun (button, _) ->
          //   Engine.Joystick.IsPushState(0, button)
          // )
          inputs |> Seq.tryFind (fst >> Engine.Keyboard.IsPushState)

        match input with
        | None -> yield ()
        | Some (_, msg) ->
          updater.Dispatch(msg)
          enabledSlideInput <- false
          yield! Coroutine.sleep Consts.inputInterval
          enabledSlideInput <- true
          yield()
    })