module RouteTiles.Core.Types.SubMenu.MainMenu

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Effects

open Affogato

open EffFs
open EffFs.Library

[<Struct; RequireQualifiedAccess>]
type SoloGameMode = TimeAttack | ScoreAttack

[<Struct; RequireQualifiedAccess>]
type Mode =
  | SoloGame of SoloGameMode
  | VS
  | Ranking
  | Achievement
  | Setting
with
  static member TimeAttack = SoloGame SoloGameMode.TimeAttack
  static member ScoreAttack = SoloGame SoloGameMode.ScoreAttack

  member this.IsEnabled = this |> function
    | Achievement | VS -> false
    | _ -> true

let modePlace = array2D [|
  [| Mode.TimeAttack; Mode.ScoreAttack; Mode.VS |]
  [| Mode.Ranking; Mode.Achievement; Mode.Setting |]
|]

[<Struct>]
type State = {
  config: Config
  cursor: int Vector2
} with
  static member Init(config) = {
    config = config
    cursor = Vector.zero
  }

  static member StateOut(_) = Eff.marker<Mode>

  member s.Mode =
    modePlace.[s.cursor.y, s.cursor.x]

[<Struct; RequireQualifiedAccess>]
type Msg =
  | Dir of Dir
  | Enter

let inline update msg (state: State) = eff {
  match msg with
  | Msg.Enter ->
    if state.Mode.IsEnabled then
      do! SoundEffect.Select
      return StateMachine.Completed state.Mode
    else
      do! SoundEffect.Invalid
      return StateMachine.Pending state

  | Msg.Dir dir ->
    let dirVec = Dir.toVector dir
    let nc = state.cursor + dirVec

    if Array2D.inside nc.y nc.x modePlace then
      do! SoundEffect.Move
      return { state with cursor = nc } |> StateMachine.Pending
    else
      return state |> StateMachine.Pending
}
