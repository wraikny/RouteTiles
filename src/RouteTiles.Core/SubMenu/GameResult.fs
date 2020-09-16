module RouteTiles.Core.SubMenu.GameResult

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects
open RouteTiles.Core.SubMenu

open EffFs
open EffFs.Library.StateMachine

[<Struct>]
type SendToServer = Yes | No

module SendToServer =
  let items = [|
    Yes
    No
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

type Response = Result<int64 * SimpleRankingsServer.Data<Ranking.Data>[], exn>

type WaitingResponse = WaitingResponse with
  static member StateOut(_) = Eff.marker<Response>

type OutStatus = StateStatus<State, GameNextSelection>

and State =
  | ResultWithSendToServerSelectState of Config * Ranking.Data * ListSelector.State<SendToServer>
  | WaitingResponseState of WaitingResponse * (Response -> OutStatus)
  | RankingListViewState of SinglePage.State<int64 * SimpleRankingsServer.Data<Ranking.Data>[]> * (unit -> OutStatus)
  | ErrorViewState of SinglePage.State<exn> * (unit -> OutStatus)
  | GameNextSelectionState of ListSelector.State<GameNextSelection> * (GameNextSelection voption -> OutStatus)
with
  static member Init(config, data) =
    let selector = ListSelector.State<SendToServer>.Init(SendToServer.Yes, SendToServer.items)
    ResultWithSendToServerSelectState (config, data, selector)

  static member StateEnter(s, k) = RankingListViewState(s, k)
  static member StateEnter(s, k) = ErrorViewState(s, k)
  static member StateEnter(s, k) = GameNextSelectionState(s, k)

  static member StateOut(_) = Eff.marker<GameNextSelection>

[<Struct>]
type Msg =
  | Incr
  | Decr
  | Enter
  | ReceiveRanking of Response

module Msg =
  let toListSelectorMsg = function
    | Incr -> ValueSome ListSelector.Msg.Incr
    | Decr -> ValueSome ListSelector.Msg.Decr
    | Enter -> ValueSome ListSelector.Msg.Enter
    | ReceiveRanking(_) -> ValueNone

  let toSinglePageMsg = function
    | Enter -> ValueSome SinglePage.Msg.Enter
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
  | ResultWithSendToServerSelectState (config, data, selector) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return Pending state
    | ValueSome msg ->
      match! ListSelector.update msg selector with
      | Pending state ->
        return
          (ResultWithSendToServerSelectState (config, data, state))
          |> Pending

      | Completed(ValueSome SendToServer.No) ->
        return! getGameNextSelection()

      | Completed(ValueSome SendToServer.Yes) ->
        match! WaitingResponse |> stateEnter with
        | Error e ->
          do! SinglePage.SinglePageState e |> stateEnter
          return! getGameNextSelection()

        | Ok res ->
          do! SinglePage.SinglePageState res |> stateEnter
          return! getGameNextSelection()

      | Completed ValueNone -> return failwith "Unexpected!"

  | WaitingResponseState(s, k) ->
    match msg with
    | Msg.ReceiveRanking data -> return k data
    | _ -> return Pending state

  | RankingListViewState(s, k) ->
    match msg |> Msg.toSinglePageMsg with
    | ValueSome m -> return stateMap (SinglePage.update m) (s, k)
    | ValueNone -> return Pending state

  | ErrorViewState(s, k) ->
    match msg |> Msg.toSinglePageMsg with
    | ValueSome m -> return stateMap (SinglePage.update m) (s, k)
    | ValueNone -> return Pending state

  | GameNextSelectionState(s, k) ->
    match msg |> Msg.toListSelectorMsg with
    | ValueNone -> return Pending state
    | ValueSome m -> return! stateMapEff (ListSelector.update m) (s, k)
}
