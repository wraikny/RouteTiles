module RouteTiles.Core.MenuV2

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.SubMenu
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

  let into = function
    | TimeAttack2000 -> SoloGame.Mode.TimeAttack 2000
    | ScoreAttack180 -> SoloGame.Mode.ScoreAttack 180


type GameResult = {
  Name: string
  Time: float32
  Point: int
  SlideCount: int
}

[<Struct>]
type PauseSelect =
  | Continue
  | ChangeController
  | Restart
  | Quit

module PauseSelect =
  let items = [|
    Continue
    ChangeController
    Restart
    Quit
  |]

[<Struct>]
type ControllerSelect =
  | ControllerSelectToPlay of toPlay:ListSelector.State<Controller>
  | ControllerSelectFromPause of fromPause:ListSelector.State<Controller>
  | ControllerSelectWhenRejected of rejected:ListSelector.State<Controller>
with
  static member StateOut(_) = Eff.marker<Controller voption>

  member s.Value = s |> function
    | ControllerSelectToPlay state -> state
    | ControllerSelectFromPause state -> state
    | ControllerSelectWhenRejected state -> state

module ControllerSelect =
  let map f state =
    match state with
    | ControllerSelectToPlay state ->
      f state |> ControllerSelectToPlay

    | ControllerSelectFromPause state ->
      f state |> ControllerSelectFromPause

    | ControllerSelectWhenRejected state ->
      f state |> ControllerSelectWhenRejected

  let inline update (msg: ListSelector.Msg) (state) = eff {
    let inline apply f state =
      ListSelector.update msg state
      |> Eff.map (StateStatus.mapPending f)

    match state with
    | ControllerSelectWhenRejected _ when msg = ListSelector.Msg.Cancel ->
      return state |> Pending

    | ControllerSelectToPlay state ->
      return! state |> apply ControllerSelectToPlay

    | ControllerSelectFromPause state ->
      return! state |> apply ControllerSelectFromPause

    | ControllerSelectWhenRejected state ->
      return! state |> apply ControllerSelectWhenRejected
  }


type WithState<'s> = WithContext<State, 's>

and WSListSelector<'item> = WithState<ListSelector.State<'item>>

and State =
  | MainMenuState of Config * ListSelector.State<Mode>
  | GameModeSelectState of WSListSelector<GameMode> * (State * GameMode voption -> State)
  | ControllerSelectState of ControllerSelect * (Controller voption -> State)
  | GameState of Config * Controller * GameMode
  | PauseState of WSListSelector<PauseSelect> * (State * PauseSelect voption -> State)
  | SettingMenuState of Setting.State * (Config voption -> State)
with
  member x.IsStringInputMode = x |> function
    | SettingMenuState (s, _) -> s.IsStringInputMode
    | _ -> false

  static member Init(config) =
    MainMenuState(config, ListSelector.State<_>.Init(0, Mode.items))

  static member StateEnter(s, k) = GameModeSelectState (s, k)
  static member StateEnter(s, k) = ControllerSelectState (s, k)
  static member StateEnter(s, k) = PauseState (s, k)
  static member StateEnter(s, k) = SettingMenuState (s, k)

let equal a b = (a, b) |> function
  | MainMenuState(a1, a2), MainMenuState(b1, b2) -> (a1, a2) = (b1, b2)
  | GameModeSelectState(a, _), GameModeSelectState(b, _) -> a = b
  | ControllerSelectState(a, _), ControllerSelectState(b, _) -> a = b
  | GameState(a1, a2, a3), GameState(b1, b2, b3) -> (a1, a2, a3) = (b1, b2, b3)
  | PauseState(a, _), PauseState(b, _) -> a = b
  | SettingMenuState(a, _), SettingMenuState(b, _) -> Setting.equal a b
  | _ -> false

[<Struct>]
type Msg =
  | Incr
  | Decr
  | Enter
  | Cancel
  | MsgOfInput of msgInput:StringInput.Msg
  | PauseGame
  | QuitGame
  | FinishGame of SoloGame.Model * time:float32
  | UpdateControllers of Controller[]
  | SelectController


module Msg =
  let toSettingMsg = function
    | Decr -> ValueSome Setting.Msg.Decr
    | Incr -> ValueSome Setting.Msg.Incr
    | Enter -> ValueSome Setting.Msg.Enter
    | Cancel -> ValueSome Setting.Msg.Cancel
    | MsgOfInput m -> ValueSome <| Setting.Msg.MsgOfInput m
    | _ -> ValueNone

  let toListSelectorMsg = function
    | Incr -> ValueSome ListSelector.Msg.Incr
    | Decr -> ValueSome ListSelector.Msg.Decr
    | Enter -> ValueSome ListSelector.Msg.Enter
    | Cancel -> ValueSome ListSelector.Msg.Cancel
    | _ -> ValueNone


let inline update (msg: Msg) (state: State): Eff<State, _> = eff {
  match msg, state with
  | _, MainMenuState(config, mainMenu) ->
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
            ListSelector.State<_>.Init(0, GameMode.items)
            |> WithContext
            |> stateEnter with
          | _, ValueNone -> return state
          | gameModeState, ValueSome gameMode ->
            let! controllers = CurrentControllers
            match!
              ListSelector.State<_>.Init(0, controllers)
              |> ControllerSelectToPlay
              |> stateEnter with
            | ValueNone -> return gameModeState
            | ValueSome controller ->
              do! GameControlEffect.Start(gameMode |> GameMode.into, controller)
              return GameState (config, controller, gameMode)

        | ValueSome Mode.Ranking ->
          return Utils.Todo(state)

        | ValueSome Mode.Setting ->
          match!
            Setting.State.Init config
            |> stateEnter with
          | ValueNone -> return state
          | ValueSome c ->
            if c <> config then
              do! SaveConfig(c)

            return MainMenuState (c, mainMenu)

        | _ ->
          return state

  | PauseGame, GameState (config, controller, gameMode) ->
    do! GameControlEffect.Pause

    match!
      ListSelector.State<_>.Init(0, PauseSelect.items)
      |> WithContext
      |> stateEnter
      with
    | _, ValueNone
    | _, ValueSome Continue ->
      do! GameControlEffect.Resume
      return state

    | _, ValueSome Restart ->
      do! GameControlEffect.Restart
      do! GameControlEffect.Resume
      return state

    | _, ValueSome Quit ->
      do! GameControlEffect.Resume
      do! GameControlEffect.Quit
      return State.Init (config)

    | pauseState, ValueSome ChangeController ->
      let! controllers = CurrentControllers
      match!
        ListSelector.State<_>.Init(controller, controllers,currentItem=controller)
        |> ControllerSelectFromPause
        |> stateEnter
        with
      | ValueNone -> return pauseState
      | ValueSome controller ->
        do! GameControlEffect.SetController controller
        return GameState (config, controller, gameMode)

  | QuitGame, GameState (config, _, _) ->
    return State.Init (config)

  | SelectController, GameState (_, controller, _) ->
    let! controllers = CurrentControllers
    let! selectedController =
      ListSelector.State<_>.Init(controller, controllers,currentItem=controller)
      |> ControllerSelectWhenRejected
      |> stateEnter

    /// force unwrap
    do! GameControlEffect.SetController selectedController.Value

    return state

  // todo: when controller is rejected
  | _, GameState _ ->
    return state

  | _, GameModeSelectState (s, k) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! WithContext.mapEff state (ListSelector.update msg) (s, k)

  | UpdateControllers controllers, ControllerSelectState (s, k) ->
    let controllerSelect = s |> ControllerSelect.map(fun s ->
      let cursorItem = s.selection.[s.cursor]
      let currentItem = s.current |> Option.map(fun i -> s.selection.[i])
      ListSelector.State<_>.Init(cursorItem, controllers, ?currentItem=currentItem)
    )
    return ControllerSelectState (controllerSelect, k)

  | _, ControllerSelectState (s, k) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! stateMapEff (ControllerSelect.update msg) (s, k)

  | _, PauseState (s, k) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! WithContext.mapEff state (ListSelector.update msg) (s, k)

  | _, SettingMenuState (s, k) ->
    match Msg.toSettingMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! stateMapEff (Setting.update msg) (s, k)
}

#if DEBUG
type Handler = Handler with
  static member inline Handle(x) = x
  static member inline Handle(e, k) = handle (e, k)
  static member inline Handle(_: SoundEffect, k) = failwith "" |> k
  static member inline Handle(_: CurrentControllers, k) = failwith "" |> k
  static member inline Handle(_: GameControlEffect, k) = failwith "" |> k
  static member inline Handle(_: SaveConfig, k) = failwith "" |> k
  // static member inline Handle(_: GetStateEffect<'a>, k) = k Unchecked.defaultof<'a>

let update' msg state =
  update msg state
  |> Eff.handle Handler
#endif
