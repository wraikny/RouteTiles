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

  type State =
    SinglePage.State<Config * SoloGame.GameMode * Response>

  let init x: State = SinglePage.SinglePageState x

  type Waiting = Waiting with
    static member StateOut(_) = Eff.marker<Response>

module Rankings =
  type Response = Result<Map<SoloGame.GameMode, SRSData[]>, exn>

  type State = SinglePage.State<Config * SoloGame.GameMode * SRSData[]>

  let init x: State = SinglePage.SinglePageState x

  type Waiting = Waiting with
    static member StateOut(_) = Eff.marker<Response>
