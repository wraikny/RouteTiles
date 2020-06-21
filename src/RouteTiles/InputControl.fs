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
  inputs |> Array.tryFind(fun ((button: JoystickButtonType, state), _) ->
    Engine.Joystick.GetButtonState(index, button) = state
  )
  |> Option.map snd


module Board =
  open RouteTiles.Core
  open RouteTiles.Core.Board

  let keyboardMapping =
    let pairs = seq {
      Keys.W, Dir.Up
      Keys.D, Dir.Right
      Keys.S, Dir.Down
      Keys.A, Dir.Left
    }
    
    [|
      for (key, dir) in pairs do
        yield [|Keys.RightShift, ButtonState.Hold; key, ButtonState.Push|], Msg.Slide dir
        yield [|key, ButtonState.Push|], Msg.MoveCursor dir
    |]

  let joystickMapping = [|
    (JoystickButtonType.DPadUp, ButtonState.Push), Msg.MoveCursor Dir.Up
    (JoystickButtonType.DPadRight, ButtonState.Push), Msg.MoveCursor Dir.Right
    (JoystickButtonType.DPadDown, ButtonState.Push), Msg.MoveCursor Dir.Down
    (JoystickButtonType.DPadLeft, ButtonState.Push), Msg.MoveCursor Dir.Left
    (JoystickButtonType.RightUp, ButtonState.Push), Msg.Slide Dir.Up
    (JoystickButtonType.RightRight, ButtonState.Push), Msg.Slide Dir.Right
    (JoystickButtonType.RightDown, ButtonState.Push), Msg.Slide Dir.Down
    (JoystickButtonType.RightLeft, ButtonState.Push), Msg.Slide Dir.Left
  |]

  let getKeyboardInput = getKeyboardInput keyboardMapping
  let getJoystickInput = getJoystickInput joystickMapping

module Pause =
  // open RouteTiles.Core.Pause

  let keyboardMapping = [|
    // [|Keys.Escape, ButtonState.Push|], Msg.Resume
  |]
