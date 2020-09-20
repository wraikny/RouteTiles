module RouteTiles.Core.SubMenu.ListSelector
open RouteTiles.Core

[<Struct>]
type State<'item> = {
  cursor: int
  current: int option
  selection: 'item[]
} with
  static member Init (cursor, selection, ?current) = {
    cursor = cursor
    selection = selection
    current = current
  }

  static member Init (cursorItem, selection, ?currentItem) =
    let cursor = Array.tryFindIndex ((=) cursorItem) selection |> Option.defaultValue 0
    let current =
      currentItem
      |> Option.map (fun x -> Array.tryFindIndex ((=) x) selection |> Option.defaultValue 0)

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
    do! SoundEffect.Move
    return { state with cursor = state.cursor - 1 } |> Pending

  | Msg.Incr when state.cursor < state.selection.Length - 1 ->
    do! SoundEffect.Move
    return { state with cursor = state.cursor + 1 } |> Pending

  | Msg.Decr | Msg.Incr ->
    do! SoundEffect.Invalid
    return state |> Pending

  | Msg.Enter ->
    do! SoundEffect.Select
    return state.selection.[state.cursor] |> ValueSome |> Completed

  | Msg.Cancel ->
    do! SoundEffect.Cancel
    return ValueNone |> Completed
}
