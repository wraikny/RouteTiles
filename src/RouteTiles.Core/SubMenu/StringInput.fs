module RouteTiles.Core.Types.SubMenu.StringInput
open RouteTiles.Core

[<Struct>]
type State = {
  inputs: char[]
  current: string
  maxLength: int
} with
  static member Init (str: string, length) = {
    current = str
    maxLength = length
    inputs = str.ToCharArray()
  }

[<Struct>]
type Msg =
  | Input of char
  | Delete
  | Enter
  | Cancel

let setInputs inputs state =
  { state with
      inputs = inputs
      current = System.String (inputs)
  }

open RouteTiles.Core.Effects
open EffFs
open EffFs.Library.StateMachine

type State with
  static member StateOut(_) = Eff.marker<string voption>

let inline update msg state = eff {
  match msg with
  | Input c when state.inputs.Length < state.maxLength ->
    let inputs = Array.addToHead c state.inputs
    return
      state
      |> setInputs inputs
      |> Pending

  | Delete when state.inputs.Length > 0 ->
    let (_, inputs) = state.inputs |> Array.tryPopLast
    return
      state
      |> setInputs inputs
      |> Pending

  | Input _ | Delete ->
    do! SoundEffect.Invalid
    return state |> Pending

  | Enter ->
    do! SoundEffect.Select
    return state.current |> ValueSome |> Completed

  | Cancel ->
    return Completed ValueNone
}
