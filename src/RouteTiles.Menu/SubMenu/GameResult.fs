module RouteTiles.Menu.SubMenu.GameResult

open RouteTiles.Common.Types
open RouteTiles.Menu
open RouteTiles.Menu.Types
open RouteTiles.Menu.Effects
open RouteTiles.Menu.SubMenu

open EffFs
open EffFs.Library.StateMachine

[<Struct>]
type SendToServer = Yes | No

module SendToServer =
  let items = [|
    No
    Yes
  |]

[<Struct; RequireQualifiedAccess>]
type GameNextSelection =
  | Restart
  | Quit

module GameNextSelection =
  let items = [|
    GameNextSelection.Quit
    GameNextSelection.Restart
  |]
type OutStatus = StateStatus<State, GameNextSelection>

and State =
  | ResultWithSendToServerSelectState of Config * GameMode * RankingData * ListSelector.State<SendToServer>
  | WaitingResponseState of Ranking.GameResult.Waiting * (Ranking.GameResult.Response -> OutStatus)
  | RankingListViewState of Ranking.State * (unit -> OutStatus)
  | ErrorViewState of SinglePage.State<exn> * (unit -> OutStatus)
  | GameNextSelectionState of ListSelector.State<GameNextSelection> * (GameNextSelection voption -> OutStatus)
  | InputName of StringInput.State * (string voption -> OutStatus)
with
  static member Init(config, gameMode, data: RankingData) =
    match gameMode with
    // オンラインランキング対応モード
    | GameMode.TimeAttack5000
    | GameMode.ScoreAttack180 ->
      let selector = ListSelector.State<SendToServer>.Init(SendToServer.Yes, SendToServer.items)
      ResultWithSendToServerSelectState (config, gameMode, data, selector)

    | GameMode.Endless ->
      let selector = ListSelector.State<GameNextSelection>.Init(GameNextSelection.Quit, GameNextSelection.items)
      GameNextSelectionState(selector,
        function
        | ValueNone -> failwith "Unexpected!"
        | ValueSome selection -> Completed selection
      )

  static member StateEnter(s, k) = WaitingResponseState(s, k)
  static member StateEnter(s, k) = RankingListViewState(s, k)
  static member StateEnter(s, k) = ErrorViewState(s, k)
  static member StateEnter(s, k) = GameNextSelectionState(s, k)
  static member StateEnter(s, k) = InputName(s, k)

  static member StateOut(_) = Eff.marker<GameNextSelection>


let equal a b = (a, b) |> function
  | ResultWithSendToServerSelectState(a1, a2, a3, a4), ResultWithSendToServerSelectState(b1, b2, b3, b4) ->
    (a1, a2, a3, a4) = (b1, b2, b3, b4)
  | WaitingResponseState(a, _), WaitingResponseState(b, _) -> a = b
  | RankingListViewState(a, _), RankingListViewState(b, _) -> a = b
  | ErrorViewState(a, _), ErrorViewState(b, _) -> a = b
  | GameNextSelectionState(a, _), GameNextSelectionState(b, _) -> a = b
  | InputName(a, _), InputName(b, _) -> a = b
  | _ -> false


[<Struct>]
type Msg =
  | Incr
  | Decr
  | Enter
  | Cancel
  | MsgOfInput of msgInput:StringInput.Msg
  | ReceiveRanking of Ranking.GameResult.Response

module Msg =
  let toListSelectorMsg = function
    | Incr -> ValueSome ListSelector.Msg.Incr
    | Decr -> ValueSome ListSelector.Msg.Decr
    | Enter -> ValueSome ListSelector.Msg.Enter
    | _ -> ValueNone

  let toSinglePageMsg = function
    | Enter
    | Cancel ->
      ValueSome SinglePage.Msg.Enter
    | _ -> ValueNone

  let toRankingMsg = function
    | Enter | Cancel -> ValueSome Ranking.Msg.Enter
    | Incr -> ValueSome Ranking.Msg.Incr
    | Decr -> ValueSome Ranking.Msg.Decr
    | _ -> ValueNone

let inline getGameNextSelection () = eff {
  match!
    ListSelector.State<GameNextSelection>.Init(GameNextSelection.Quit, GameNextSelection.items)
    |> stateEnter
      with
  | ValueSome x -> return Completed x
  | ValueNone -> return failwith "Unexpected!"
}

let inline update msg state = eff {
  match state with
  | ResultWithSendToServerSelectState (config, gameMode, data, selector) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return Pending state
    | ValueSome msg ->
      match! ListSelector.update msg selector with
      | Pending state ->
        return
          (ResultWithSendToServerSelectState (config, gameMode, data, state))
          |> Pending

      | Completed(ValueSome SendToServer.No) ->
        return! getGameNextSelection()

      | Completed(ValueSome SendToServer.Yes) ->
        let inline cont name = eff {
          let data = { data with Name = name }

          do! GameRankingEffect.InsertSelect(config.guid, gameMode, data)

          match! Ranking.GameResult.Waiting |> stateEnter with
          | Ok(id, data) ->
            do! Ranking.State.Init (ValueSome id, config, gameMode, data) |> stateEnter
            return! getGameNextSelection()
          | Error e ->
            // TODO
            do! SinglePage.SinglePageState e |> stateEnter
            return! getGameNextSelection()
        }

        if config.name.IsNone then
          match!
            StringInput.State.Init("", Setting.NameMaxLength) |> stateEnter
            with
          | ValueSome name when name <> "" -> return! cont name
          | _ -> return Pending state
        else
          return! cont config.name.Value

      | Completed ValueNone -> return failwith "Unexpected!"

  | WaitingResponseState(_s, k) ->
    match msg with
    | Msg.ReceiveRanking data -> return k data
    | _ -> return Pending state

  | RankingListViewState(s, k) ->
    match msg |> Msg.toRankingMsg with
    | ValueSome m -> return! stateMapEff (Ranking.update m) (s, k)
    | ValueNone -> return Pending state

  | ErrorViewState(s, k) ->
    match msg |> Msg.toSinglePageMsg with
    | ValueSome m -> return! stateMapEff (SinglePage.update m) (s, k)
    | ValueNone -> return Pending state

  | GameNextSelectionState(s, k) ->
    match msg |> Msg.toListSelectorMsg with
    | ValueNone -> return Pending state
    | ValueSome m -> return! stateMapEff (ListSelector.update m) (s, k)

  | InputName (s, k) ->
    let inline f msg = stateMapEff (StringInput.update msg) (s, k)
    match msg with
    | Msg.MsgOfInput (StringInput.Msg.Enter) when s.current = "" ->
      do! SoundEffect.Invalid
      return Pending state
    | Msg.MsgOfInput msg -> return! f msg
    | Msg.Cancel -> return! f StringInput.Msg.Cancel
    | _ -> return Pending state
}
