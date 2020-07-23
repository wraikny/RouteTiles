module RouteTiles.Core.Pause

open RouteTiles.Core.Types.Pause

[<Struct; RequireQualifiedAccess>]
type Msg = OpenPause | Decr | Incr | QuitPause | Select

let private fromSbyte = function
  | 0y -> Model.ContinueGame
  | 1y -> Model.RestartGame
  | 2y -> Model.QuitGame
  | 3y -> Model.NotPaused
  | _ -> failwith "Unexpected"

let private toSbyte = function
  | Model.ContinueGame -> 0y
  | Model.RestartGame -> 1y
  | Model.QuitGame -> 2y
  | Model.NotPaused -> 3y

let update (msg: Msg) (model: Model) =
  (model, msg) |> function
  | _, Msg.Select -> Model.NotPaused
  | _, Msg.OpenPause -> Model.ContinueGame
  | _, Msg.QuitPause | _, Msg.Select -> Model.NotPaused
  | m, Msg.Decr -> (toSbyte m - 1y + 3y) % 3y |> fromSbyte
  | m, Msg.Incr -> (toSbyte m + 1y + 3y) % 3y |> fromSbyte

let isPauseActivated a b =
  match a, b with
  | _ when a = b -> ValueNone
  | Model.NotPaused, _ -> ValueSome true
  | _, Model.NotPaused -> ValueSome false
  | _ -> ValueNone
