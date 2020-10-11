module RouteTiles.Menu.Effects

open EffFs
open RouteTiles.Common.Types
open RouteTiles.Menu.Types

[<Struct; RequireQualifiedAccess>]
type GameControlEffect =
  | Pause
  | Resume
  | Start of GameMode * Controller * Config
  | Restart
  | Quit
with
  static member Effect(_) = Eff.marker<unit>

[<Struct>]
type SetControllerEffect = SetController of Controller
with
  static member Effect(_) = Eff.marker<bool>

type CurrentControllers = CurrentControllers with
  static member Effect(_) = Eff.marker<Controller[]>

[<Struct; RequireQualifiedAccess>]
type SoundEffect =
  | Select
  | Cancel
  | Move
  | Invalid
  | InputChar
  | DeleteChar
with
  static member Effect(_) = Eff.marker<unit>

[<Struct>]
type GameStartEffect = GameStartEffect of GameMode * Controller
with
  static member Effect(_) = Eff.marker<unit>

[<Struct>]
type GameRankingEffect =
  | SelectAll
  | InsertSelect of System.Guid * GameMode * RankingData
with
  static member Effect(_) = Eff.marker<unit>

[<Struct>]
type SaveConfig = SaveConfig of Config
with
  static member Effect(_) = Eff.marker<unit>

[<Struct>]
type SetSoundVolumeEffect = SetSoundVolume of float32 * float32
with
  static member Effect(_) = Eff.marker<unit>

[<Struct>]
type ErrorLogEffect = ErrorLogEffect of exn
with
  static member Effect(_) = Eff.marker<unit>
