module RouteTiles.Core.Types.SoloGame

open RouteTiles.Core.Types

[<Struct; RequireQualifiedAccess>]
type Mode =
  | TimeAttack of score:int
  | ScoreAttack of sec:float32

type Model = {
  controller: Controller
  mode: Mode
  board: Board.Model
}