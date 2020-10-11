module RouteTiles.Menu.SubMenu.Ranking

open RouteTiles.Common.Types
open RouteTiles.Menu
open RouteTiles.Menu.Types
open RouteTiles.Menu.Effects
open RouteTiles.Menu.SubMenu

open EffFs
open EffFs.Library.StateMachine

type SRSData = SimpleRankingsServer.Data<RankingData>

module GameResult =
  type Response = Result<int64 * SRSData[], exn>

  // type State =
  //   SinglePage.State<Config * GameMode * Response>

  // let init x: State = SinglePage.SinglePageState x

  type Waiting = Waiting with
    static member StateOut(_) = Eff.marker<Response>

module Rankings =
  type Response = Result<Map<GameMode, SRSData[]>, exn>

  // type State = SinglePage.State<Config * GameMode * SRSData[]>

  // let init x: State = SinglePage.SinglePageState x

  type Waiting = Waiting with
    static member StateOut(_) = Eff.marker<Response>


let [<Literal>] OnePageItemCount = 5

type State = {
  page: int
  config: Config
  gameMode: GameMode
  insertedId: int64 voption
  data: SRSData[]
} with
  static member StateOut(_) = Eff.marker<unit>

  static member Init(insertedId, config, gameMode, data) =
    let page =
      insertedId |> function
      | ValueNone -> 0
      | ValueSome id ->
        data
        |> Array.tryFindIndex (fun (x: SRSData) -> x.id = id)
        |> function
        | None -> 0
        | Some x -> x / OnePageItemCount

    { page = page
      config = config
      gameMode = gameMode
      insertedId = insertedId
      data = data
    }

  member state.IsIncrementable = state.page < (state.data.Length - 1) / OnePageItemCount
  member state.IsDecrementable = 0 < state.page



[<RequireQualifiedAccess>]
type Msg =
  | Enter
  | Incr
  | Decr


let inline update msg (state: State) = eff {
  match msg with
  | Msg.Enter -> return Completed ()

  | Msg.Incr ->
    if state.IsIncrementable then
      do! SoundEffect.Move
      return Pending { state with page = state.page + 1 }
    else
      do! SoundEffect.Invalid
      return Pending state

  | Msg.Decr ->
    if state.IsDecrementable then
      do! SoundEffect.Move
      return Pending { state with page = state.page - 1 }
    else
      do! SoundEffect.Invalid
      return Pending state
}
