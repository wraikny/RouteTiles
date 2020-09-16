module RouteTiles.Core.SubMenu.SinglePage

open RouteTiles.Core
open RouteTiles.Core.Effects
open RouteTiles.Core.SubMenu

open EffFs
open EffFs.Library.StateMachine

type State<'a> = SinglePageState of 'a
with
  static member StateOut(_) = Eff.marker<unit>

[<Struct; RequireQualifiedAccess>]
type Msg = Enter

let inline update msg _state: Eff<StateStatus<State<'a>, unit>, ^h> = eff {
  match msg with
  | Msg.Enter -> return Completed ()
}

