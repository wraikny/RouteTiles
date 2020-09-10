module RouteTiles.Core.Types.MenuV2
open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.SubMenu
open RouteTiles.Core.Effects

open EffFs
open EffFs.Library

type GameMode =
  | TimeAttack2000
  | ScoreAttack180

type State =
  | MainMenuState of MainMenu.State
  // | GameModeSelectState
  // | ControllerSelectState
  | SettingMenuState of Setting.State * (Config voption -> State)
with
  static member StateEnter(s, k) = SettingMenuState (s, k)

  member x.IsStringInputMode = x |> function
    | SettingMenuState (s, _) -> s.IsStringInputMode
    | _ -> false

  static member Init(config) =
    MainMenuState <| MainMenu.State.Init(config, 0)


[<Struct>]
type Msg =
  | Incr
  | Decr
  | Enter
  | Cancel
  | MsgOfInput of msgInput:StringInput.Msg


module Msg =
  let toSettingMsg = function
    | Decr -> Setting.Msg.Decr
    | Incr -> Setting.Msg.Incr
    | Enter -> Setting.Msg.Enter
    | Cancel -> Setting.Msg.Cancel
    | MsgOfInput m -> Setting.Msg.MsgOfInput m

  let toListSelectorMsg = function
    | Incr -> ValueSome ListSelector.Msg.Incr
    | Decr -> ValueSome ListSelector.Msg.Decr
    | Enter -> ValueSome ListSelector.Msg.Enter
    | Cancel -> ValueSome ListSelector.Msg.Cancel
    | _ -> ValueNone


let inline update (msg: Msg) (state: State): Eff<State, _> = eff {
  match state with
  | MainMenuState s ->
    match Msg.toListSelectorMsg msg with
    // Cancelは拾う
    | ValueNone
    | ValueSome ListSelector.Msg.Cancel ->
      return state

    | ValueSome msg ->
      match! MainMenu.update msg s with
      | StateMachine.Pending s -> return MainMenuState s

      | StateMachine.Completed mode ->
        match mode with
        | ValueSome MainMenu.Mode.GamePlay ->
          return Utils.Todo(state)

        | ValueSome MainMenu.Mode.Ranking ->
          return Utils.Todo(state)

        | ValueSome MainMenu.Mode.Setting ->
          match!
            Setting.State.Init (s.config)
            |> StateMachine.stateEnter with
          | ValueNone -> return state
          | ValueSome config -> return MainMenuState { s with config = config }

        | _ ->
          return state

  | SettingMenuState (s, k) ->
    let msg = Msg.toSettingMsg msg
    return! StateMachine.stateMapEff (Setting.update msg) (s, k)
}

#if DEBUG
type Handler = Handler with
  static member inline Handle(x) = x
  static member inline Handle(e, k) = StateMachine.handle (e, k)
  static member inline Handle(_: SoundEffect, k) = k ()

let update' msg state =
  update msg state
  |> Eff.handle Handler
#endif
