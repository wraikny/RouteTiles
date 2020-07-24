namespace RouteTiles.App

open RouteTiles.Core.Types
open RouteTiles.Core.Types.Pause
open RouteTiles.Core.Pause

open Altseed2

type PauseNode(dispatch) =
  inherit Node()

  let coroutineNode = CoroutineNode(IsUpdated = false)

  let mutable controller = ValueNone
  let mutable pauseModel = Model.NotPaused
  let mutable isInputEnabled = false

  do
    base.AddChildNode(coroutineNode)

    base.AddChildNode(
      RectangleNode(
        RectangleSize = Consts.ViewCommon.windowSize.To2F(),
        Color = Consts.GameCommon.pauseBackground,
        ZOrder = ZOrder.Pause.background,
        IsDrawn = false
      )
    )

  do
    coroutineNode.Add(seq {
      while true do
        if isInputEnabled then
          let input =
            controller |> function
            | ValueSome Controller.Keyboard ->
              InputControl.Pause.getKeyboardInput()
            | ValueSome (Controller.Joystick(_, ValidJoystickIndex index)) ->
              InputControl.Pause.getJoystickInput(index)
            | _ -> None
          
          match input with
          | Some msg -> dispatch msg
          | None -> ()

        yield()
    })

  member this.OnNext(model: SoloGame.Model) =
    if controller.IsNone then
      controller <- ValueSome model.controller

    isPauseActivated pauseModel model.pause
    |> ValueOption.iter(fun t ->
        this.SetPause(t)
    )

    pauseModel <- model.pause

#if DEBUG
    if pauseModel <> Model.NotPaused then
      printfn "%A" pauseModel
#endif

  member private this.SetPause(isPaused) =
    if isPaused then
      coroutineNode.Add(seq {
        yield! Coroutine.sleep Consts.GameCommon.waitingInputIntervalOnOpeningPause
        isInputEnabled <- true
        yield()
      })
    else
      isInputEnabled <- false

    coroutineNode.IsUpdated <- isPaused
    for child in this.Children do
      match child with
      | :? DrawnNode as d -> d.IsDrawn <- isPaused
      | _ -> ()