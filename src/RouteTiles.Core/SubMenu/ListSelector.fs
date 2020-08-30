module RouteTiles.Core.Types.SubMenu.ListSelector
open RouteTiles.Core

[<Struct>]
type State<'item> = {
  cursor: int
  current: int
  selection: 'item[]
} with
  static member Init (cursor, selection) = {
    cursor = cursor
    current = cursor
    selection = selection
  }

  static member Init (item, selection) =
    let cursor = Array.findIndex ((=) item) selection
    {
      cursor = cursor
      current = cursor
      selection = selection
    }


[<Struct>]
type Msg =
  | Decr
  | Incr
  | Enter


open EffFs
open EffFs.Library
open RouteTiles.Core.Effects

type State<'a> with
  static member StateOut(_) = Eff.marker<'a>

let inline update msg state = eff {
  match msg with
  | Msg.Decr when state.cursor > 0 ->
    return { state with cursor = state.cursor - 1 } |> StateMachine.Pending

  | Msg.Incr when state.cursor < state.selection.Length - 1 ->
    return { state with cursor = state.cursor + 1 } |> StateMachine.Pending

  | Msg.Decr | Msg.Incr ->
    do! SoundEffect.Invalid
    return state |> StateMachine.Pending

  | Msg.Enter ->
    return state.selection.[state.cursor] |> StateMachine.Completed
}
