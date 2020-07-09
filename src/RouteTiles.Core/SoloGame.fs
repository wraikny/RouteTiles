module RouteTiles.Core.SoloGame

open RouteTiles.Core

[<Struct; RequireQualifiedAccess>]
type Mode =
  | TimeAttack
  | ScoreAttack

type Model = {
  controller: Controller
  mode: Mode
  board: Board.Model.Board
}

[<Struct; RequireQualifiedAccess>]
type Msg =
  | Board of board:Board.Msg
  | SetController of controller:Controller
with
  static member inline Lift(msg) = Msg.Board msg

open EffFs

let inline init config mode controller = eff {
  let! board = Board.Model.Board.init config
  return {
    controller = controller
    mode = mode
    board = board
  }
}

let inline update (msg: Msg) (model: Model) =
  msg |> function
  | Msg.Board(msg) ->
    eff {
      let! board = Board.Update.update msg model.board
      return { model with board = board }
    }

  | Msg.SetController controller ->
    { model with controller = controller }
    |> Eff.pure'

