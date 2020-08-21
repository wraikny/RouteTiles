module RouteTiles.App.InputControl

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

open RouteTiles.Core.Types

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
