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

type Model = {
  cursor: Mode
}

type Msg =
  | MoveMode of dir:Dir


let initModel = {
  cursor = Mode.TimeAttack
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

let update msg model =
  msg |> function
  | MoveMode dir ->
    { model with
        cursor =
          (Dir.toVector dir) + (Mode.toVec model.cursor)
          |> Mode.fromVec
    }
