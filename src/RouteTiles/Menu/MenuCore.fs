module RouteTiles.App.MenuCore
open RouteTiles.Core.Types.Common

open Affogato

[<Struct; RequireQualifiedAccess>]
type Mode =
  | TimeAttack
  | ScoreAttack
  | VS
  | Ranking
  | Achievement
  | Setting
with
  member this.IsEnabled = this |> function
    | VS -> false
    | _ -> true

type Model = {
  cursor: Mode
  selected: bool
}

[<Struct; RequireQualifiedAccess>]
type Msg =
  | MoveMode of dir:Dir
  | Select
  | Back


let initModel = {
  cursor = Mode.TimeAttack
  selected = false
}


module Mode =
  let toVec mode =
    let (x, y) = mode |> function
      | Mode.TimeAttack -> (0, 0)
      | Mode.ScoreAttack -> (1, 0)
      | Mode.VS -> (2, 0)
      | Mode.Ranking -> (0, 1)
      | Mode.Achievement -> (1, 1)
      | Mode.Setting -> (2, 1)
    Vector2.init x y

  let fromVec (v: int Vector2) =
    let x = (v.x + 3) % 3
    let y = (v.y + 2) % 2

    (x, y) |> function
    | (0, 0) -> Mode.TimeAttack
    | (1, 0) -> Mode.ScoreAttack
    | (2, 0) -> Mode.VS
    | (0, 1) -> Mode.Ranking
    | (1, 1) -> Mode.Achievement
    | (2, 1) -> Mode.Setting
    | a -> failwithf "invalid input: %A" a

open EffFs

type SoundInvalidEffect = SoundInvalidEffect with
  static member Effect(_) = Eff.output<unit>

let inline update msg model = eff {
  match msg with
  | Msg.MoveMode dir ->
    return
      { model with
          cursor =
            (Dir.toVector dir) + (Mode.toVec model.cursor)
            |> Mode.fromVec
      }

  | Msg.Select ->
    if model.selected then
      return model
    else
      if model.cursor.IsEnabled then
        return { model with selected = true }
      else
        do! SoundInvalidEffect
        return model

  | Msg.Back ->
    if model.selected then
      return { model with selected = false }
    else
      return model
}
