namespace RouteTiles.App

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI

open MenuCore



open RouteTiles.Core.Types.Common

module MenuInput =
  open MenuCore

  let keyboard =
    [|
      for (key, _, _, dir) in InputControl.dirPairs -> [|key, ButtonState.Push|], Msg.MoveMode dir
      yield [|Key.Space, ButtonState.Push|], Msg.Select
      yield [|Key.Enter, ButtonState.Push|], Msg.Select
      yield [|Key.Escape, ButtonState.Push|], Msg.Back
      yield [|Key.Backspace, ButtonState.Push|], Msg.Back
    |]

  let joystick = [|
    for (_, btnL, _, dir) in InputControl.dirPairs -> (btnL, ButtonState.Push), Msg.MoveMode dir
    yield (JoystickButton.RightRight, ButtonState.Push), Msg.Select
    yield (JoystickButton.RightDown, ButtonState.Push), Msg.Back
    yield (JoystickButton.Guide, ButtonState.Push), Msg.Back
  |]

open RouteTiles.Core.Utils

open EffFs

[<Struct>]
type MenuHandler = MenuHandler with
  static member inline Handle(x) = x

  static member inline Handle(SoundEffect _kind, k) =
    k()

  static member inline Handle(CurrentControllers, k) =
    [|
      yield Controller.Keyboard
      for i in 0..15 do
        let info = Engine.Joystick.GetJoystickInfo i
        if info <> null && info.IsGamepad then
          yield Controller.Joystick(i, info.GamepadName, info.GUID)
    |] |> k

  static member inline Handle(GameStartEffect soloGameMode, k) =
    k ()

type Menu() =
  inherit Node()

  let mutable prevModel = ValueNone
  let updater = Updater<MenuCore.Model, MenuCore.Msg>()

  let coroutine = CoroutineNode()
  let uiRoot = BoxUIRootNode()

  do
    base.AddChildNode coroutine
    base.AddChildNode uiRoot

    updater.Subscribe(fun model ->
      prevModel
      |> ValueOption.map((=) model)
      |> ValueOption.defaultValue false
      |> function
      | true -> ()
      | false ->
        prevModel <- ValueSome model
        uiRoot.ClearElement()
        uiRoot.SetElement <| MenuView.menu model
    ) |> ignore

  let mutable prevControllerCount = Engine.Joystick.ConnectedJoystickCount

  let getKeyboardInput = InputControl.getKeyboardInput MenuInput.keyboard
  let getJoystickInput = InputControl.getJoystickInput MenuInput.joystick

  override __.OnUpdate() =
    // Controllerの接続状況の更新
    if updater.Model.Value.state.ControllerRefreshEnabled then
      let curretConnectedCount = Engine.Joystick.ConnectedJoystickCount
      if curretConnectedCount <> prevControllerCount then
        prevControllerCount <- curretConnectedCount
        let controllers = MenuHandler.Handle(CurrentControllers, id)
        updater.Dispatch(Msg.RefreshController controllers) |> ignore

    getKeyboardInput ()
    |> Option.alt(fun () ->
      let count = Engine.Joystick.ConnectedJoystickCount
      seq {
        for i in 0..count-1 do
          let info = Engine.Joystick.GetJoystickInfo(i)
          if info.IsGamepad then
            match getJoystickInput i with
            | Some x -> yield x
            | _ -> ()
      }
      |> Seq.tryHead
    )
    |> Option.iter (fun msg ->
      updater.Dispatch msg
      |> ignore
    )

  override __.OnAdded() =
    prevModel <-
      ( initModel,
        fun msg model ->
          printfn "Msg: %A" msg
          MenuCore.update msg model
          |> Eff.handle MenuHandler
      )
      |> updater.Init
      |> ValueSome
