module RouteTiles.Core.SoloGame

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.SoloGame
open RouteTiles.Core.Effects

[<Struct; RequireQualifiedAccess>]
type Msg =
  | Board of board:Board.Msg
  | SetController of controller:Controller
  | PauseMsg of pause:Pause.Msg
with
  static member inline Lift(msg) = Msg.Board msg

  static member inline Lift(msg) = Msg.PauseMsg msg
 
let inline isPaused { pause = pause } = pause <> Pause.Model.NotPaused

open EffFs

let inline init config mode controller = eff {
  let! board = Board.Model.init config
  return {
    controller = controller
    mode = mode
    board = board

    pause = Pause.Model.NotPaused
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

  | Msg.PauseMsg pauseMsg ->
    eff {
      match pauseMsg, model.pause with
      | _, Pause.Model.QuitGame -> do! ControlEffect.Quit
      | Pause.Msg.OpenPause, Pause.Model.NotPaused -> do! ControlEffect.SetIsPaused true
      | Pause.Msg.Select, Pause.Model.ContinueGame -> do! ControlEffect.SetIsPaused false
      | Pause.Msg.Select, Pause.Model.RestartGame -> do! ControlEffect.Restart
      | _ -> ()

      return { model with pause = Pause.update pauseMsg model.pause }
    }
