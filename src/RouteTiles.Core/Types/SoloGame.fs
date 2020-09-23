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
  | TimeAttack2000
  | ScoreAttack180
  | Endless

module GameMode =
  let items = [|
    TimeAttack2000
    ScoreAttack180
    Endless
  |]

  let selected = TimeAttack2000

  let into = function
    | Endless -> Mode.Endless
#if DEBUG
    | TimeAttack2000 -> Mode.TimeAttack 2000
    | ScoreAttack180 -> Mode.ScoreAttack 180
#else
    | TimeAttack2000 -> Mode.TimeAttack 2000
    | ScoreAttack180 -> Mode.ScoreAttack 180
#endif
