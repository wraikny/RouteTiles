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


  let characterInput =
    [|
      let mutable c = 'a'
      let mutable C = 'A'
      for i in (int Key.A)..(int Key.Z) do
        let key = enum<Key> i
        yield [| Key.RightShift, ButtonState.Hold;  key, ButtonState.Push |], C
        yield [| Key.LeftShift, ButtonState.Hold;  key, ButtonState.Push |], C
        yield [| key, ButtonState.Push|], c
        c <- char(int c + 1)
        C <- char(int C + 1)

      c <- '0'
      for i in (int Key.Num0)..(int Key.Num9) do
        let key = enum<Key> i
        yield [| key, ButtonState.Push |], c
        c <- char (int c + 1)

      c <- '0'
      for i in (int Key.Keypad0)..(int Key.Keypad9) do
        let key = enum<Key> i
        yield [| key, ButtonState.Push |], c
        c <- char (int c + 1)
    |]
    |> Array.map (fun (keys, c) -> (keys, StringInput.Input c))
    |> Array.append [|
      [| Key.Enter, ButtonState.Push |], StringInput.Enter
      [| Key.Backspace, ButtonState.Push |], StringInput.Delete
      [| Key.Delete, ButtonState.Push |], StringInput.Delete
    |]
    |> Array.map (fun (keys, c) -> (keys, Msg.InputName c))
    |> Array.append [|
      [| Key.Escape, ButtonState.Push |], Msg.Back
    |]

module MenuV2 =
  open RouteTiles.Core.SubMenu
  open RouteTiles.Core.MenuV2

  let keyboard =
    [|
      yield [|Key.W, ButtonState.Push|], Msg.Decr
      yield [|Key.S, ButtonState.Push|], Msg.Incr
      yield [|Key.Space, ButtonState.Push|], Msg.Enter
      yield [|Key.Enter, ButtonState.Push|], Msg.Enter
      yield [|Key.Escape, ButtonState.Push|], Msg.Cancel
    |]

  let joystick = [|
    yield (JoystickButton.DPadUp, ButtonState.Push), Msg.Decr
    yield (JoystickButton.DPadDown, ButtonState.Push), Msg.Incr
    yield (JoystickButton.RightRight, ButtonState.Push), Msg.Enter
    yield (JoystickButton.RightDown, ButtonState.Push), Msg.Cancel
    yield (JoystickButton.Guide, ButtonState.Push), Msg.Cancel
  |]

  let keys (a: Key) (b: Key) = [| for i in (int a)..(int b) -> enum<Key> i |]
  let chars (a: char) (b: char) = [| a .. b|]

  let characterInput =
    [|
      yield!
        Array.zip3
          (keys Key.A Key.Z)
          (chars 'a' 'z')
          (chars 'A' 'Z')
        |> Array.map(fun (key, c, C) ->
          [|
            [| Key.RightShift, ButtonState.Hold;  key, ButtonState.Push |], C
            [| Key.LeftShift, ButtonState.Hold;  key, ButtonState.Push |], C
            [| key, ButtonState.Push|], c
          |]
        )
        |> Array.concat

      yield!
        Array.zip
          (keys Key.Num0 Key.Num9)
          (chars '0' '9')
        |> Array.map (fun (key, c) -> [| key, ButtonState.Push |], c)

      yield!
        Array.zip
          (keys Key.Keypad0 Key.Keypad9)
          (chars '0' '9')
        |> Array.map(fun (key, c) -> [| key, ButtonState.Push |], c)
    |]
    |> Array.map (fun (keys, c) -> (keys, StringInput.Input c))
    |> Array.append [|
      [| Key.Enter, ButtonState.Push |], StringInput.Enter
      [| Key.Backspace, ButtonState.Push |], StringInput.Delete
      [| Key.Delete, ButtonState.Push |], StringInput.Delete
    |]
    |> Array.map (fun (keys, c) -> (keys, Msg.MsgOfInput c))
    |> Array.append [|
      [| Key.Escape, ButtonState.Push |], Msg.Cancel
    |]
