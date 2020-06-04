[<AutoOpen>]
module RouteTiles.App.AltseedExtension

open Altseed

type Keyboard with
  member inline x.IsFreeState(key) = x.GetKeyState(key) = ButtonState.Free
  member inline x.IsPushState(key) = x.GetKeyState(key) = ButtonState.Push
  member inline x.IsHoldState(key) = x.GetKeyState(key) = ButtonState.Hold
  member inline x.IsReleaseState(key) = x.GetKeyState(key) = ButtonState.Release
