module RouteTiles.Core.Types.SubMenu.Setting

open EffFs
open EffFs.Library

open RouteTiles.Core
open RouteTiles.Core.Effects
open RouteTiles.Core.Types.SubMenu

[<Struct; RequireQualifiedAccess>]
type Mode =
  | InputName
  | Background
  | Enter

let modes = [|
  Mode.InputName
  Mode.Background
  Mode.Enter
|]

let NameMaxLength = 12

type SettingState = {
  mode: Mode
  config: Config
  initConfig: Config
  background: ListSelector.State<Background>
}

type State =
  | InputName of StringInput.State * (string -> StateMachine.StateStatus<State, Config>)
  | Base of SettingState
with
  static member Init (initConfig: Config) =
    Base
      { mode = Mode.InputName
        config = initConfig
        initConfig = initConfig
        background = ListSelector.State<_>.Init(initConfig.background, Background.items)
      }

  static member StateEnter(s, k) = InputName (s, k)
  static member StateOut(_) = Eff.marker<Config>



[<Struct; RequireQualifiedAccess>]
type Msg =
  | PrevMode
  | NextMode
  | Enter
  | Cancel
  | Decr
  | Incr
  | MsgOfInput of msgInput: StringInput.Msg

let inline update msg state = eff {
  match state, msg with
  // InputName
  | InputName (s, k), Msg.MsgOfInput msg ->
    return!
      StateMachine.stateMapEff (StringInput.update msg) (s, k)
  | InputName _,_ ->
    return state |> StateMachine.Pending

  // Mode Change
  | Base s, msg when msg = Msg.NextMode || msg = Msg.PrevMode ->
    let index = modes |> Array.findIndex ((=) s.mode)
    let newIndex =
      (index + if msg = Msg.NextMode then +1 else -1)
      |> max 0
      |> min (modes.Length - 1)

    let mode = modes.[newIndex]

    return StateMachine.Pending (
      if mode = s.mode then state
      else Base { s with mode = mode }
    )

  // Cancelallation
  | Base { initConfig = config }, Msg.Cancel ->
    return StateMachine.Completed config

  // Enter
  | Base { mode = Mode.Enter; config = config }, Msg.Enter ->
    return StateMachine.Completed config

  // InputName
  | Base ({ mode = Mode.InputName; config = { name = name } } as s), Msg.Enter ->
    let name = name |> ValueOption.defaultValue ""
    let! inputName = StateMachine.stateEnter (StringInput.State.Init (name, NameMaxLength))

    return StateMachine.Pending (
      if inputName = name then state
      else Base { s with config = { s.config with name = ValueSome inputName } }
    )

  // Background
  | Base s, msg when s.mode = Mode.Background ->
    let msg = msg |> function
      | Msg.Decr -> ValueSome ListSelector.Decr
      | Msg.Incr -> ValueSome ListSelector.Incr
      | Msg.Enter -> ValueSome ListSelector.Enter
      | _ -> ValueNone

    match msg with
    | ValueNone ->
      return state |> StateMachine.Pending

    | ValueSome msg ->
      let! state = ListSelector.update msg s.background

      return state |> function
        | StateMachine.Pending x ->
          Base { s with background = x }

        | StateMachine.Completed x ->
          Base { s with config = { s.config with background = x } }

      |> StateMachine.Pending

  | _ -> return state |> StateMachine.Pending
}
