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

open RouteTiles.Core.Types

module Board =
  open RouteTiles.Core.Types.Board
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

module Pause =
  open RouteTiles.Core.Types
  open RouteTiles.Core.Pause

  let keyboard = [|
    [|Keys.Escape, ButtonState.Push|], Msg.QuitPause
    [|Keys.W, ButtonState.Push|], Msg.Decr
    [|Keys.Up, ButtonState.Push|], Msg.Decr
    [|Keys.S, ButtonState.Push|], Msg.Incr
    [|Keys.Down, ButtonState.Push|], Msg.Incr
    [|Keys.Space, ButtonState.Push|], Msg.Select
    [|Keys.Enter, ButtonState.Push|], Msg.Select
  |]

  let joystick = [|
    yield!
      seq { JoystickButtonType.Start; JoystickButtonType.Guide }
      |> Seq.map(fun a -> (a, ButtonState.Push), Msg.QuitPause)
    (JoystickButtonType.DPadUp, ButtonState.Push), Msg.Decr
    (JoystickButtonType.DPadDown, ButtonState.Push), Msg.Incr
    (JoystickButtonType.RightRight, ButtonState.Push), Msg.Select
  |]

  let getKeyboardInput = getKeyboardInput keyboard
  let getJoystickInput = getJoystickInput joystick

module SoloGame =
  open RouteTiles.Core
  open RouteTiles.Core.SoloGame

  let keyboard = [|
    yield [|Keys.Escape, ButtonState.Push|], Msg.PauseMsg Pause.Msg.OpenPause
    for (button, msg) in Board.keyboardMapping do
      yield (button, Msg.Board msg)
  |]

  let joystick = [|
    yield!
      seq { JoystickButtonType.Start; JoystickButtonType.Guide }
      |> Seq.map(fun a -> (a, ButtonState.Push), Msg.PauseMsg Pause.Msg.OpenPause)
    yield! Board.joystickMapping |> Seq.map(fun (a, b) -> (a, Msg.Board b))
  |]

  let getKeyboardInput = getKeyboardInput keyboard
  let getJoystickInput = getJoystickInput joystick
