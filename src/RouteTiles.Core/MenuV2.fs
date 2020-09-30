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
  | HowTo
  | Setting

module Mode =
  let items = [|
    Mode.GamePlay
    Mode.Ranking
    Mode.HowTo
    Mode.Setting
  |]



[<Struct>]
type ControllerSelect =
  | ControllerSelectToPlay of toPlay:ListSelector.State<Controller>
  // | ControllerSelectFromPause of fromPause:ListSelector.State<Controller>
  | ControllerSelectWhenRejected of rejected:ListSelector.State<Controller>
with
  static member StateOut(_) = Eff.marker<Controller voption>

  member s.Value = s |> function
    | ControllerSelectToPlay state -> state
    | ControllerSelectWhenRejected state -> state

module ControllerSelect =
  let map f state =
    match state with
    | ControllerSelectToPlay state ->
      f state |> ControllerSelectToPlay

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

    | ControllerSelectWhenRejected state ->
      return! state |> apply ControllerSelectWhenRejected
  }

[<RequireQualifiedAccess>]
type HowToMode =
  | KeyboardShift
  | KeyboardSeparate
  | Joystick
  | Game
  | Slide
  | Route
  | Loop
  | Point
  | Board


type WithState<'s> = WithContext<State, 's>

and WSListSelector<'item> = WithState<ListSelector.State<'item>>

and State =
  | MainMenuState of Config * ListSelector.State<Mode>
  | GameModeSelectState of WSListSelector<SoloGame.GameMode> * (State * SoloGame.GameMode voption -> State)
  | ControllerSelectState of WithState<ControllerSelect> * (State * Controller voption -> State)
  | GameState of Config * Controller * SoloGame.GameMode
  | GameResultState of GameResult.State * (GameResult.GameNextSelection -> State)
  | PauseState of Pause.State * (Pause.PauseResult -> State)
  | SettingMenuState of Setting.State * (Config voption -> State)
  | RankingState of Ranking.State * (unit -> State)
  | WaitingResponseState of Ranking.Rankings.Waiting * (Ranking.Rankings.Response -> State)
  | ErrorViewState of SinglePage.State<exn> * (unit -> State)
  | HowToState of SinglePage.State<HowToMode> * (unit -> State)
with
  member x.IsStringInputMode = x |> function
    | SettingMenuState (s, _) -> s.IsStringInputMode
    | _ -> false

  static member inline Init(config) = MainMenuState(config, ListSelector.State<_>.Init(0, Mode.items))

  static member StateEnter(s, k) = GameModeSelectState (s, k)
  static member StateEnter(s, k) = ControllerSelectState (s, k)
  static member StateEnter(s, k) = GameResultState (s, k)
  static member StateEnter(s, k) = PauseState (s, k)
  static member StateEnter(s, k) = SettingMenuState (s, k)
  static member StateEnter(s, k) = RankingState (s, k)
  static member StateEnter(s, k) = WaitingResponseState (s, k)
  static member StateEnter(s, k) = ErrorViewState (s, k)
  static member StateEnter(s, k) = HowToState (s, k)

let equal a b = (a, b) |> function
  | MainMenuState(a1, a2), MainMenuState(b1, b2) -> (a1, a2) = (b1, b2)
  | GameModeSelectState(a, _), GameModeSelectState(b, _) -> a = b
  | ControllerSelectState(a, _), ControllerSelectState(b, _) -> a = b
  | GameState(a1, a2, a3), GameState(b1, b2, b3) -> (a1, a2, a3) = (b1, b2, b3)
  | GameResultState(a, _), GameResultState(b, _) -> GameResult.equal a b
  | PauseState(a, _), PauseState(b, _) -> Pause.equal a b
  | SettingMenuState(a, _), SettingMenuState(b, _) -> Setting.equal a b
  | RankingState(a, _), RankingState(b, _) -> a = b
  | WaitingResponseState(a, _), WaitingResponseState(b, _) -> a = b
  | ErrorViewState(a, _), ErrorViewState(b, _) -> a = b
  | HowToState(a, _), HowToState(b, _) -> a = b
  | _ -> false


type Msg =
  | Incr
  | Decr
  | Enter
  | Cancel
  | Right
  | Left
  | MsgOfInput of msgInput:StringInput.Msg
  | PauseGame
  | QuitGame
  | FinishGame of SoloGame.Model * time:float32
  | UpdateControllers of Controller[]
  | SelectController
  | ReceiveRankingGameResult of Ranking.GameResult.Response
  | ReceiveRankingRankings of Ranking.Rankings.Response
  | OpenHowToControl


module Msg =
  let toSettingMsg = function
    | Decr -> ValueSome Setting.Msg.Decr
    | Incr -> ValueSome Setting.Msg.Incr
    | Right -> ValueSome Setting.Msg.Right
    | Left -> ValueSome Setting.Msg.Left
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
    | ReceiveRankingGameResult data -> ValueSome (GameResult.Msg.ReceiveRanking data)
    | MsgOfInput m -> ValueSome (GameResult.Msg.MsgOfInput m)
    | _ -> ValueNone

  let toPauseMsg = function
    | Incr -> ValueSome Pause.Msg.Incr
    | Decr -> ValueSome Pause.Msg.Decr
    | Enter -> ValueSome Pause.Msg.Enter
    | Cancel -> ValueSome Pause.Msg.Cancel
    | UpdateControllers x -> ValueSome (Pause.Msg.UpdateControllers x)
    | _ -> ValueNone

  let toSinglePageMsg = function
    | Enter | Cancel -> ValueSome SinglePage.Msg.Enter
    | _ -> ValueNone

  let toRankingMsg = function
    | Incr -> ValueSome Ranking.Msg.Incr
    | Decr -> ValueSome Ranking.Msg.Decr
    | Enter | Cancel -> ValueSome Ranking.Msg.Enter
    | _ -> ValueNone

let inline update (msg: Msg) (state: State): Eff<State, _> = eff {
  match msg, state with
  | Msg.OpenHowToControl, _ ->
    do! SinglePage.SinglePageState HowToMode.KeyboardSeparate |> stateEnter
    do! SinglePage.SinglePageState HowToMode.KeyboardShift |> stateEnter
    do! SinglePage.SinglePageState HowToMode.Joystick |> stateEnter
    return state

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
            ListSelector.State<_>.Init(SoloGame.GameMode.selected, SoloGame.GameMode.items)
            |> WithContext
            |> stateEnter with
          | _, ValueNone ->
            do! SoundEffect.Cancel
            return state
          
          | gameModeState, ValueSome gameMode ->
            let! controllers = CurrentControllers
            match!
              ListSelector.State<_>.Init(0, controllers)
              |> ControllerSelectToPlay
              |> WithContext
              |> stateEnter with
            | _, ValueNone ->
              do! SoundEffect.Cancel
              return gameModeState

            | _, ValueSome controller ->
              do! GameControlEffect.Start(gameMode |> SoloGame.GameMode.into, controller, config)
              return GameState (config, controller, gameMode)

        | ValueSome Mode.Ranking ->
          do! GameRankingEffect.SelectAll

          match! SubMenu.Ranking.Rankings.Waiting |> stateEnter with
          | Error e ->
            do! SinglePage.SinglePageState e |> stateEnter
            do! ErrorLogEffect e
            return state
          | Ok dataMap ->
            let gameMode = SoloGame.GameMode.ScoreAttack180
            do! Ranking.State.Init(ValueNone, config, gameMode, dataMap.[gameMode]) |> stateEnter
            do! SoundEffect.Move

            let gameMode = SoloGame.GameMode.TimeAttack5000
            do! Ranking.State.Init(ValueNone, config, gameMode, dataMap.[gameMode]) |> stateEnter
            do! SoundEffect.Move

            return state

        | ValueSome Mode.HowTo ->
          do! SinglePage.SinglePageState HowToMode.Game |> stateEnter
          do! SinglePage.SinglePageState HowToMode.Board |> stateEnter
          do! SinglePage.SinglePageState HowToMode.Slide |> stateEnter
          do! SinglePage.SinglePageState HowToMode.Route |> stateEnter
          do! SinglePage.SinglePageState HowToMode.Loop |> stateEnter
          do! SinglePage.SinglePageState HowToMode.Point |> stateEnter
          do! SinglePage.SinglePageState HowToMode.KeyboardSeparate |> stateEnter
          do! SinglePage.SinglePageState HowToMode.KeyboardShift |> stateEnter
          do! SinglePage.SinglePageState HowToMode.Joystick |> stateEnter
          return state

        | ValueSome Mode.Setting ->
          match!
            Setting.State.Init config
            |> stateEnter with
          | ValueNone ->
            do! SetSoundVolume(config.bgmVolume, config.seVolume)
            do! SoundEffect.Cancel
            return state

          | ValueSome c ->
            if c <> config then
              do! SaveConfig(c)

            return MainMenuState (c, mainMenu)

        | _ ->
          return state

  | PauseGame, GameState (config, controller, gameMode) ->
    do! GameControlEffect.Pause

    match!
      Pause.State.Init (controller)
      |> stateEnter
      with
    | Pause.PauseResult.Continue(c) ->
      do! GameControlEffect.Resume
      return GameState(config, c, gameMode)

    | Pause.PauseResult.Restart(c) ->
      do! GameControlEffect.Restart
      do! GameControlEffect.Resume
      return GameState(config, c, gameMode)

    | Pause.PauseResult.Quit ->
      do! GameControlEffect.Quit
      do! GameControlEffect.Resume
      return State.Init(config)

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

    match!
      GameResult.State.Init(config, gameMode, model, time)
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

  // | Msg.Cancel, (PauseState (_, k) as state) -> return k (state, Pause.PauseResult.Continue)
  // | Msg.PauseGame, (PauseState (_, k) as state) -> return k (state, Pause.PauseResult.Continue)
  | _, PauseState (s, k) ->
    match Msg.toPauseMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! stateMapEff (Pause.update msg) (s, k)

  | _, SettingMenuState (s, k) ->
    match Msg.toSettingMsg msg with
    | ValueNone -> return state
    | ValueSome msg -> return! stateMapEff (Setting.update msg) (s, k)

  | _, RankingState (s, k) ->
    match Msg.toRankingMsg msg with
    | ValueSome msg ->
      return! stateMapEff (Ranking.update msg) (s, k)
    | _ -> return state

  | _, ErrorViewState(s, k) ->
    match Msg.toSinglePageMsg msg with
    | ValueSome msg ->
      return! stateMapEff (SinglePage.update msg) (s, k)
    | _ -> return state

  | _, HowToState(s, k) ->
    match Msg.toSinglePageMsg msg with
    | ValueSome msg ->
      return! stateMapEff (SinglePage.update msg) (s, k)
    | _ -> return state

  | _, WaitingResponseState(_s, k) ->
    match msg with
    | ReceiveRankingRankings data -> return k data
    | _ -> return state
}

#if DEBUG
module Debug =
  type Handler = Handler with
    static member inline Handle(x) = x
    static member inline Handle(e, k) = handle (e, k)
    static member inline Handle(_: SoundEffect, k) = failwith "" |> k
    static member inline Handle(_: CurrentControllers, k) = failwith "" |> k
    static member inline Handle(_: GameControlEffect, k) = failwith "" |> k
    static member inline Handle(_: SetControllerEffect, k) = failwith "" |> k
    static member inline Handle(_: SaveConfig, k) = failwith "" |> k
    static member inline Handle(_: GameRankingEffect, k) = failwith "" |> k
    static member inline Handle(_: SetSoundVolumeEffect, k) = failwith "" |> k
    static member inline Handle(_: ErrorLogEffect, k) = failwith "" |> k
    // static member inline Handle(_: GetStateEffect<'a>, k) = k Unchecked.defaultof<'a>

  let update' msg state =
    update msg state
    |> Eff.handle Handler
#endif
