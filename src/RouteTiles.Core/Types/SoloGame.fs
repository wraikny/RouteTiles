module RouteTiles.Core.Types.SoloGame

open RouteTiles.Core.Types

[<Struct; RequireQualifiedAccess>]
type Mode =
  | TimeAttack
  | ScoreAttack

type Model = {
  controller: Controller
  mode: Mode
  board: Board.Model

  pause: Pause.Model
}