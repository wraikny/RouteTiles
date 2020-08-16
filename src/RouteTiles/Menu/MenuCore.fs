module RouteTiles.App.MenuCore
open RouteTiles.Core.Types.Common

open Affogato

[<Struct>]
type Mode =
  | TimeAttack
  | ScoreAttack
  | VS
  | Ranking
  | Achievement
  | Setting

type ModeDescr = {
  name: string
  description: string
}

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
      | TimeAttack -> (0, 0)
      | ScoreAttack -> (1, 0)
      | VS -> (2, 0)
      | Ranking -> (0, 1)
      | Achievement -> (1, 1)
      | Setting -> (2, 1)
    Vector2.init x y

  let fromVec (v: int Vector2) =
    let x = (v.x + 3) % 3
    let y = (v.y + 2) % 2

    (x, y) |> function
    | (0, 0) -> TimeAttack
    | (1, 0) -> ScoreAttack
    | (2, 0) -> VS
    | (0, 1) -> Ranking
    | (1, 1) -> Achievement
    | (2, 1) -> Setting
    | a -> failwithf "invalid input: %A" a

let update msg model =
  msg |> function
  | MoveMode dir ->
    { model with
        cursor =
          (Dir.toVector dir) + (Mode.toVec model.cursor)
          |> Mode.fromVec
    }
