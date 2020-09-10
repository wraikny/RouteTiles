module RouteTiles.Core.Types.SubMenu.ListSelector
open RouteTiles.Core

[<Struct>]
type State<'item> = {
  cursor: int
  selection: 'item[]
  current: int voption
} with
  static member Init (cursor, selection, current) = {
    cursor = cursor
    selection = selection
    current = current
  }

  static member Init (cursorItem, selection, currentItem) =
    let cursor = Array.findIndex ((=) cursorItem) selection
    let current =
      currentItem
      |> ValueOption.map (fun x -> Array.findIndex ((=) x) selection)

    {
      cursor = cursor
      current = current
      selection = selection
    }


[<Struct>]
type Msg =
  | Decr
  | Incr
  | Enter
  | Cancel


open EffFs
open EffFs.Library.StateMachine
open RouteTiles.Core.Effects

type State<'a> with
  static member StateOut(_: State<'a>) = Eff.marker<'a voption>

let inline update msg state = eff {
  match msg with
  | Msg.Decr when state.cursor > 0 ->
    return { state with cursor = state.cursor - 1 } |> Pending

  | Msg.Incr when state.cursor < state.selection.Length - 1 ->
    return { state with cursor = state.cursor + 1 } |> Pending

  | Msg.Decr | Msg.Incr ->
    do! SoundEffect.Invalid
    return state |> Pending

  | Msg.Enter ->
    return state.selection.[state.cursor] |> ValueSome |> Completed

  | Msg.Cancel ->
    return ValueNone |> Completed
}
