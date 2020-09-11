module RouteTiles.Core.Types.MenuV2
open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.SubMenu
open RouteTiles.Core.Effects

open EffFs
open EffFs.Library.StateMachine

[<Struct; RequireQualifiedAccess>]
type Mode =
  | GamePlay
  | Ranking
  | Setting

module Mode =
  let items = [|
    Mode.GamePlay
    Mode.Ranking
    Mode.Setting
  |]

type GameMode =
  | TimeAttack2000
  | ScoreAttack180

module GameMode =
  let items = [|
    TimeAttack2000
    ScoreAttack180
  |]

type State =
  | MainMenuState of Config * ListSelector.State<Mode>
  | GameModeSelectState of WithContext<State, ListSelector.State<GameMode>> * (State * GameMode voption -> State)
  | ControllerSelectState of WithContext<State, ListSelector.State<Controller>> * (State * Controller voption -> State)
  | SettingMenuState of Setting.State * (Config voption -> State)
with
  member x.IsStringInputMode = x |> function
    | SettingMenuState (s, _) -> s.IsStringInputMode
    | _ -> false

  static member Init(config) =
    MainMenuState(config, ListSelector.State<_>.Init(0, Mode.items, ValueNone))

  static member StateEnter(s, k) = GameModeSelectState (s, k)
  static member StateEnter(s, k) = ControllerSelectState (s, k)
  static member StateEnter(s, k) = SettingMenuState (s, k)

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
  | MainMenuState(config, mainMenu) ->
    match Msg.toListSelectorMsg msg with
    // Cancelは拾う
    | ValueNone
    | ValueSome ListSelector.Msg.Cancel ->
      return state

    | ValueSome msg ->
      match! ListSelector.update msg mainMenu with
      | Pending s -> return MainMenuState (config, s)

      | Completed mode ->
        match mode with
        | ValueSome Mode.GamePlay ->
          match!
            ListSelector.State<_>.Init(0, GameMode.items, ValueNone)
            |> WithContext
            |> stateEnter with
          | _, ValueNone -> return state
          | gameModeState, ValueSome gameMode ->
            let! controllers = CurrentControllers
            match!
              ListSelector.State<_>.Init(0, controllers, ValueNone)
              |> WithContext
              |> stateEnter with
            | _, ValueNone -> return gameModeState
            | controllerState, ValueSome controller ->
              return Utils.Todo(controllerState)

        | ValueSome Mode.Ranking ->
          return Utils.Todo(state)

        | ValueSome Mode.Setting ->
          match!
            Setting.State.Init config
            |> stateEnter with
          | ValueNone -> return state
          | ValueSome c -> return MainMenuState (c, mainMenu)

        | _ ->
          return state

  | GameModeSelectState (s, k) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! WithContext.mapEff state (ListSelector.update msg) (s, k)

  | ControllerSelectState (s, k) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! WithContext.mapEff state (ListSelector.update msg) (s, k)

  | SettingMenuState (s, k) ->
    let msg = Msg.toSettingMsg msg
    return! stateMapEff (Setting.update msg) (s, k)
}

#if DEBUG
type Handler = Handler with
  static member inline Handle(x) = x
  static member inline Handle(e, k) = handle (e, k)
  static member inline Handle(_: SoundEffect, k) = failwith "" |> k
  static member inline Handle(_: CurrentControllers, k) = failwith "" |> k
  // static member inline Handle(_: GetStateEffect<'a>, k) = k Unchecked.defaultof<'a>

let update' msg state =
  update msg state
  |> Eff.handle Handler
#endif
