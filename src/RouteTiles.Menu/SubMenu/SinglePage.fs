module RouteTiles.Menu.SubMenu.SinglePage

open RouteTiles.Menu
open RouteTiles.Menu.Effects
open RouteTiles.Menu.SubMenu

open EffFs
open EffFs.Library.StateMachine

type State<'a> = SinglePageState of 'a
with
  static member StateOut(_) = Eff.marker<unit>

[<Struct; RequireQualifiedAccess>]
type Msg = Enter

let inline update msg _state = eff {
  match msg with
  | Msg.Enter ->
    do! SoundEffect.Select
    return Completed ()
}

