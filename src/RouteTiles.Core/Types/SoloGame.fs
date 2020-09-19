module RouteTiles.Core.Types.SoloGame

open RouteTiles.Core.Types

[<Struct; RequireQualifiedAccess>]
type Mode =
  | TimeAttack of score:int
  | ScoreAttack of sec:int

type Model = {
  // controller: Controller
  mode: Mode
  board: Board.Model
}

[<Struct>]
type GameMode =
  | TimeAttack2000
  | ScoreAttack180

module GameMode =
  let items = [|
    TimeAttack2000
    ScoreAttack180
  |]

  let into = function
    | TimeAttack2000 -> Mode.TimeAttack 2000
    | ScoreAttack180 -> Mode.ScoreAttack 10
