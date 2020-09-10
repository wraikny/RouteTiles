module RouteTiles.Core.Types.SubMenu.MainMenu

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects

open Affogato
open EffFs
open EffFs.Library.StateMachine

[<Struct; RequireQualifiedAccess>]
type Mode =
  | GamePlay
  | Ranking
  | Setting

let private modes = [|
  Mode.GamePlay
  Mode.Ranking
  Mode.Setting
|]

[<Struct>]
type State = {
  config: Config
  selector: ListSelector.State<Mode>
} with
  static member Init(config, cursor: int) = {
    config = config
    selector = ListSelector.State<_>.Init(cursor, modes, ValueNone)
  }

  static member StateOut(_: State) = Eff.marker<Mode>

type Msg = ListSelector.Msg

let inline update msg state = eff {
  return!
    state.selector
    |> ListSelector.update msg
    |> (Eff.map << StateStatus.mapPending) (fun s -> { state with selector = s })
}
