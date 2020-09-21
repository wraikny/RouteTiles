module RouteTiles.Core.SubMenu.Setting

open RouteTiles.Core
open RouteTiles.Core.Effects
open RouteTiles.Core.SubMenu

open EffFs
open EffFs.Library.StateMachine

[<Struct; RequireQualifiedAccess>]
type Mode =
  | InputName
  | Background
  | Enter

module Mode =
  let items = [|
    Mode.InputName
    // Mode.Background
    Mode.Enter
  |]

let NameMaxLength = 12

type SettingState = {
  config: Config
  selector: ListSelector.State<Mode>
  // background: ListSelector.State<Background>
}

type OutStatus = StateStatus<State, Config voption>

and State =
  | InputName of StringInput.State * (string voption -> OutStatus)
  | Background of ListSelector.State<Background> * (Background voption -> OutStatus)
  | Base of SettingState
with
  static member Init (initConfig: Config) =
    Base
      { config = initConfig
        selector = ListSelector.State<Mode>.Init(0, Mode.items)
        // background = ListSelector.State<_>.Init(initConfig.background, Background.items, ValueSome initConfig.background)
      }

  member x.IsStringInputMode = x |> function
    | InputName _ -> true
    | _ -> false

  static member StateOut(_: State) = Eff.marker<Config voption>
  static member StateEnter(s, k) = InputName (s, k)
  static member StateEnter(s, k) = Background (s, k)

let equal a b = (a, b) |> function
  | InputName (a, _), InputName (b, _) -> a = b
  | Background (a, _), Background (b, _) -> a = b
  | Base a, Base b -> a = b
  | _ -> false


[<Struct; RequireQualifiedAccess>]
type Msg =
  // | PrevMode
  // | NextMode
  | Enter
  | Cancel
  | Decr
  | Incr
  | MsgOfInput of msgInput: StringInput.Msg

module Msg =
  let toListSelector = function
    | Msg.Incr -> ValueSome ListSelector.Msg.Incr
    | Msg.Decr -> ValueSome ListSelector.Msg.Decr
    | Msg.Enter -> ValueSome ListSelector.Enter
    | Msg.Cancel -> ValueSome ListSelector.Cancel
    | _ -> ValueNone


let inline update msg state = eff {
  match state, msg with
  | InputName (s, k), msg ->
    let inline f msg = stateMapEff (StringInput.update msg) (s, k)
    match msg with
    | Msg.MsgOfInput msg -> return! f msg
    | Msg.Cancel -> return! f StringInput.Msg.Cancel
    | _ -> return Pending state


  | Background (s, k), msg ->
    match Msg.toListSelector msg with
    | ValueSome msg -> return! stateMapEff (ListSelector.update msg) (s, k)
    | _ -> return Pending state

  // Cancelallation
  | Base _, Msg.Cancel ->
    do! SoundEffect.Cancel
    return Completed ValueNone

  // Mode Change
  | Base ({ config = config } as s), msg ->
    match Msg.toListSelector msg with
    | ValueNone -> return Pending state
    | ValueSome msg ->
      match!
        s.selector
        |> ListSelector.update msg with
      | Completed ValueNone -> return failwith "unexpected"

      | Completed (ValueSome Mode.InputName) ->
        let name = config.name |> ValueOption.defaultValue ""

        match!
          StringInput.State.Init (name, NameMaxLength)
          |> stateEnter
          with
        | ValueNone -> return Pending state
        | ValueSome inputName ->
          let newName = if inputName = "" then ValueNone else ValueSome inputName
          return
            { s with config = { s.config with name = newName }}
            |> Base
            |> Pending

      | Completed (ValueSome Mode.Background) ->
        match!
          ListSelector.State<Background>.Init(config.background, Background.items, currentItem=config.background)
          |> stateEnter with
        | ValueNone -> return Pending state
        | ValueSome background ->
          return
            { s with config = { s.config with background = background }}
            |> Base
            |> Pending

      | Completed (ValueSome Mode.Enter) ->
        return Completed (ValueSome config)

      | Pending selector ->
        return
          { s with selector = selector }
          |> Base
          |> Pending
}
