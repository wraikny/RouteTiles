[<AutoOpen>]
module RouteTiles.App.Altseed2Extension

open Altseed2

type Keyboard with
  member inline x.IsFreeState(key) = x.GetKeyState(key) = ButtonState.Free
  member inline x.IsPushState(key) = x.GetKeyState(key) = ButtonState.Push
  member inline x.IsHoldState(key) = x.GetKeyState(key) = ButtonState.Hold
  member inline x.IsReleaseState(key) = x.GetKeyState(key) = ButtonState.Release

type Joystick with
  member inline x.IsFreeState(index, button: JoystickButton) = x.GetButtonState(index, button) = ButtonState.Free
  member inline x.IsPushState(index, button: JoystickButton) = x.GetButtonState(index, button) = ButtonState.Push
  member inline x.IsHoldState(index, button: JoystickButton) = x.GetButtonState(index, button) = ButtonState.Hold
  member inline x.IsReleaseState(index, button: JoystickButton) = x.GetButtonState(index, button) = ButtonState.Release


let mutable time = 0.0f

type Engine with
  static member InitializeEx(title, size: Vector2I, config) =
    time <- 0.0f
    if not <| Engine.Initialize(title, size.X, size.Y, config) then
      failwith "Failed to initialize the Altseed2"

  static member Run() =
    let rec loop() =
      if Engine.DoEvents() then
        time <- time + Engine.DeltaSecond
        BoxUI.BoxUISystem.Update()
        Engine.Update() |> ignore
        loop()

    loop()
  
  static member TerminateEx() =
    BoxUI.BoxUISystem.Terminate()
    Engine.Terminate()

  static member Time with get() = time
