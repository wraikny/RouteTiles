module RouteTiles.Core.Types.SoloGame

open RouteTiles.Core.Types

[<Struct; RequireQualifiedAccess>]
type Mode =
  | TimeAttack of score:int
  | ScoreAttack of sec:int
  | Endless

type Model = {
  // controller: Controller
  mode: Mode
  board: Board.Model
}

[<Struct>]
type GameMode =
  | TimeAttack5000
  | ScoreAttack180
  | Endless

module GameMode =
  let items = [|
    ScoreAttack180
    TimeAttack5000
    Endless
  |]

  let selected = ScoreAttack180

  let into = function
    | Endless -> Mode.Endless
#if DEBUG
    | TimeAttack5000 -> Mode.TimeAttack 5000
    | ScoreAttack180 -> Mode.ScoreAttack 180
#else
    | TimeAttack5000 -> Mode.TimeAttack 5000
    | ScoreAttack180 -> Mode.ScoreAttack 180
#endif
