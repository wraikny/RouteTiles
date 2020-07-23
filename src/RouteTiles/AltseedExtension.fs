[<AutoOpen>]
module RouteTiles.App.Altseed2Extension

open Altseed2

type Keyboard with
  member inline x.IsFreeState(key) = x.GetKeyState(key) = ButtonState.Free
  member inline x.IsPushState(key) = x.GetKeyState(key) = ButtonState.Push
  member inline x.IsHoldState(key) = x.GetKeyState(key) = ButtonState.Hold
  member inline x.IsReleaseState(key) = x.GetKeyState(key) = ButtonState.Release

type Joystick with
  member inline x.IsFreeState(index, button: JoystickButtonType) = x.GetButtonState(index, button) = ButtonState.Free
  member inline x.IsPushState(index, button: JoystickButtonType) = x.GetButtonState(index, button) = ButtonState.Push
  member inline x.IsHoldState(index, button: JoystickButtonType) = x.GetButtonState(index, button) = ButtonState.Hold
  member inline x.IsReleaseState(index, button: JoystickButtonType) = x.GetButtonState(index, button) = ButtonState.Release

let (|ValidJoystickIndex|_|) index =
  if Engine.Joystick.IsPresent index then
    Some (ValidJoystickIndex index)
  else
    None
