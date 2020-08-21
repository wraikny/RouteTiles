module RouteTiles.App.InputControl

open RouteTiles.Core.Types
open Altseed2

let getKeyboardInput (inputs) () : 'msg option =
  inputs
  |> Array.tryFind(fun (buttons, _) ->
    buttons |> Array.forall(fun (button, state) ->
      Engine.Keyboard.GetKeyState(button) = state
    )
  )
  |> Option.map snd

let getJoystickInput (inputs) (index): 'msg option =
  inputs |> Array.tryFind(fun ((button: JoystickButton, state), _) ->
    Engine.Joystick.GetButtonState(index, button) = state
  )
  |> Option.map snd

let pauseInput controller =
  controller |> function
  | Controller.Keyboard ->
    Engine.Keyboard.IsPushState(Key.Escape)
  | Controller.Joystick (index, _, _) ->
    Engine.Joystick.IsPushState(index, JoystickButton.Guide)


let dirPairs = [|
  Key.W, JoystickButton.DPadUp, JoystickButton.RightUp, Dir.Up
  Key.D, JoystickButton.DPadRight, JoystickButton.RightRight, Dir.Right
  Key.S, JoystickButton.DPadDown, JoystickButton.RightDown, Dir.Down
  Key.A, JoystickButton.DPadLeft, JoystickButton.RightLeft, Dir.Left
|]

module Board =
  open RouteTiles.Core.Types.Board
  open RouteTiles.Core.Board

  let keyboardMapping =
    [|
      for (key, _, _, dir) in dirPairs do
        yield [|Key.RightShift, ButtonState.Hold; key, ButtonState.Push|], Msg.Slide dir
        yield [|key, ButtonState.Push|], Msg.MoveCursor dir
    |]

  let joystickMapping = [|
    for (_, btnL, btnR, dir) in dirPairs do
    (btnL, ButtonState.Push), Msg.MoveCursor dir
    (btnR, ButtonState.Push), Msg.Slide dir
  |]

module SoloGame =
  open RouteTiles.Core
  open RouteTiles.Core.SoloGame

  let keyboard = [|
    for (button, msg) in Board.keyboardMapping do
      yield (button, Msg.Board msg)
  |]

  let joystick = [|
    yield! Board.joystickMapping |> Seq.map(fun (a, b) -> (a, Msg.Board b))
  |]

  let getKeyboardInput = getKeyboardInput keyboard
  let getJoystickInput = getJoystickInput joystick


module Menu =
  open RouteTiles.Core.Types.Menu

  let keyboard =
    [|
      for (key, _, _, dir) in dirPairs -> [|key, ButtonState.Push|], Msg.MoveMode dir
      yield [|Key.Space, ButtonState.Push|], Msg.Select
      yield [|Key.Enter, ButtonState.Push|], Msg.Select
      yield [|Key.Escape, ButtonState.Push|], Msg.Back
    |]

  let joystick = [|
    for (_, btnL, _, dir) in dirPairs -> (btnL, ButtonState.Push), Msg.MoveMode dir
    yield (JoystickButton.RightRight, ButtonState.Push), Msg.Select
    yield (JoystickButton.RightDown, ButtonState.Push), Msg.Back
    yield (JoystickButton.Guide, ButtonState.Push), Msg.Back
  |]