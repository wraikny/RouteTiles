module RouteTiles.Core.SubMenu.Ranking

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects
open RouteTiles.Core.SubMenu

open EffFs
open EffFs.Library.StateMachine

type SRSData = SimpleRankingsServer.Data<Ranking.Data>

module GameResult =
  type Response = Result<int64 * SRSData[], exn>

  // type State =
  //   SinglePage.State<Config * SoloGame.GameMode * Response>

  // let init x: State = SinglePage.SinglePageState x

  type Waiting = Waiting with
    static member StateOut(_) = Eff.marker<Response>

module Rankings =
  type Response = Result<Map<SoloGame.GameMode, SRSData[]>, exn>

  // type State = SinglePage.State<Config * SoloGame.GameMode * SRSData[]>

  // let init x: State = SinglePage.SinglePageState x

  type Waiting = Waiting with
    static member StateOut(_) = Eff.marker<Response>


let [<Literal>] OnePageItemCount = 5

type State = {
  page: int
  config: Config
  gameMode: SoloGame.GameMode
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


[<RequireQualifiedAccess>]
type Msg =
  | Enter
  | Incr
  | Decr


let inline update msg (state: State) = eff {
  match msg with
  | Msg.Enter -> return Completed ()

  | Msg.Incr ->
    if (state.data.Length - 1) / OnePageItemCount > state.page then
      do! SoundEffect.Move
      return Pending { state with page = state.page + 1 }
    else
      do! SoundEffect.Invalid
      return Pending state

  | Msg.Decr ->
    if state.page > 0 then
      do! SoundEffect.Move
      return Pending { state with page = state.page - 1 }
    else
      do! SoundEffect.Invalid
      return Pending state
}
