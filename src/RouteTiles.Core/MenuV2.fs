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

// type GameMode =
//   | TimeAttack2000
//   | ScoreAttack180

// module GameMode =
//   let items = [|
//     TimeAttack2000
//     ScoreAttack180
//   |]

//   let into = function
//     | TimeAttack2000 -> SoloGame.Mode.TimeAttack 2000
//     | ScoreAttack180 -> SoloGame.Mode.ScoreAttack 180


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
  | GameModeSelectState of WSListSelector<SoloGame.GameMode> * (State * SoloGame.GameMode voption -> State)
  | ControllerSelectState of WithState<ControllerSelect> * (State * Controller voption -> State)
  | GameState of Config * Controller * SoloGame.GameMode
  | GameResultState of GameResult.State * (GameResult.GameNextSelection -> State)
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
  static member StateEnter(s, k) = GameResultState (s, k)
  static member StateEnter(s, k) = PauseState (s, k)
  static member StateEnter(s, k) = SettingMenuState (s, k)

let equal a b = (a, b) |> function
  | MainMenuState(a1, a2), MainMenuState(b1, b2) -> (a1, a2) = (b1, b2)
  | GameModeSelectState(a, _), GameModeSelectState(b, _) -> a = b
  | ControllerSelectState(a, _), ControllerSelectState(b, _) -> a = b
  | GameState(a1, a2, a3), GameState(b1, b2, b3) -> (a1, a2, a3) = (b1, b2, b3)
  | GameResultState(a, _), GameResultState(b, _) -> GameResult.equal a b
  | PauseState(a, _), PauseState(b, _) -> a = b
  | SettingMenuState(a, _), SettingMenuState(b, _) -> Setting.equal a b
  | _ -> false


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
  | ReceiveRanking of RankingResponse


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

  let toGameResultMsg = function
    | Incr -> ValueSome GameResult.Msg.Incr
    | Decr -> ValueSome GameResult.Msg.Decr
    | Enter -> ValueSome GameResult.Msg.Enter
    | Cancel -> ValueSome GameResult.Msg.Cancel
    | ReceiveRanking data -> ValueSome (GameResult.Msg.ReceiveRanking data)
    | MsgOfInput m -> ValueSome (GameResult.Msg.MsgOfInput m)
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
            ListSelector.State<_>.Init(0, SoloGame.GameMode.items)
            |> WithContext
            |> stateEnter with
          | _, ValueNone -> return state
          | gameModeState, ValueSome gameMode ->
            let! controllers = CurrentControllers
            match!
              ListSelector.State<_>.Init(0, controllers)
              |> ControllerSelectToPlay
              |> WithContext
              |> stateEnter with
            | _, ValueNone -> return gameModeState
            | _, ValueSome controller ->
              do! GameControlEffect.Start(gameMode |> SoloGame.GameMode.into, controller)
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
        |> WithContext
        |> stateEnter
        with
      | _, ValueNone -> return pauseState
      | controllerState, ValueSome controller ->
        let! res = SetController controller
        if res then
          return GameState (config, controller, gameMode)
        else
          return controllerState

  | QuitGame, GameState (config, _, _) ->
    return State.Init (config)

  | SelectController, GameState (config, controller, gameMode) ->
    do! GameControlEffect.Pause

    let! controllers = CurrentControllers
    let! controllerState, selectedController =
      ListSelector.State<_>.Init(controller, controllers,currentItem=controller)
      |> ControllerSelectWhenRejected
      |> WithContext
      |> stateEnter

    /// force unwrap
    let! res = SetController selectedController.Value

    if res then
      do! GameControlEffect.Resume
      return GameState (config, selectedController.Value, gameMode)
    else
      do! SoundEffect.Invalid

      // あまりスマートではない
      match controllerState with
      | State.ControllerSelectState (WithContext s, k) ->
        let! controllers = CurrentControllers

        let controllerSelect =
          s |> ControllerSelect.map(fun s ->
            let cursorItem = s.selection.[s.cursor]
            let currentItem = s.current |> Option.map(fun i -> s.selection.[i])
            ListSelector.State<_>.Init(cursorItem, controllers, ?currentItem=currentItem)
          )
          |> WithContext

        return State.ControllerSelectState (controllerSelect, k)
      | _ ->
        return failwithf "Invalid State of ControllerSelectState Context: %A" controllerState


  | Msg.FinishGame (model, time), GameState(config, controller, gameMode) ->
    do! GameControlEffect.Pause

    // todo
    let data: Ranking.Data =
      { Name = ""
        Time = time
        Point = model.board.point
        SlideCount = model.board.slideCount
        TilesCount = model.board.vanishedTilesCount
        RoutesCount = model.board.routesHistory.Length
        LoopsCount = model.board.loopsHistory.Length
      }

    match!
      GameResult.State.Init(config, gameMode, data)
      |> stateEnter
        with
    | GameResult.GameNextSelection.Restart ->
      do! GameControlEffect.Restart
      do! GameControlEffect.Resume

      return GameState(config, controller, gameMode)
    | GameResult.GameNextSelection.Quit ->
      do! GameControlEffect.Quit
      do! GameControlEffect.Resume

      return State.Init (config)

  | _, GameState _ ->
    return state

  | _, GameModeSelectState (s, k) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! WithContext.mapEff state (ListSelector.update msg) (s, k)

  | UpdateControllers controllers, ControllerSelectState (WithContext s, k) ->
    let controllerSelect =
      s |> ControllerSelect.map(fun s ->
        let cursorItem = s.selection.[s.cursor]
        let currentItem = s.current |> Option.map(fun i -> s.selection.[i])
        ListSelector.State<_>.Init(cursorItem, controllers, ?currentItem=currentItem)
      )
      |> WithContext
    return ControllerSelectState (controllerSelect, k)

  | _, ControllerSelectState (s, k) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! WithContext.mapEff state (ControllerSelect.update msg) (s, k)

  | _, GameResultState (s, k) ->
    match Msg.toGameResultMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! stateMapEff (GameResult.update msg) (s, k)

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
  static member inline Handle(_: SetControllerEffect, k) = failwith "" |> k
  static member inline Handle(_: SaveConfig, k) = failwith "" |> k
  static member inline Handle(_: GameRankingEffect, k) = failwith "" |> k
  // static member inline Handle(_: GetStateEffect<'a>, k) = k Unchecked.defaultof<'a>

let update' msg state =
  update msg state
  |> Eff.handle Handler
#endif
