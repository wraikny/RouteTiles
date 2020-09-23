namespace RouteTiles.Core.Effects

open RouteTiles.Core
open RouteTiles.Core.Types

open EffFs

// [<Struct>]
// type EmitVanishParticleEffect = EmitVanishParticleEffect of Set<Types.Board.RouteOrLoop>
// with
//   static member Effect(_: EmitVanishParticleEffect) = Eff.marker<unit>

[<Struct; RequireQualifiedAccess>]
type GameControlEffect =
  | Pause
  | Resume
  | Start of SoloGame.Mode * Controller * Config
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
type GameStartEffect = GameStartEffect of SoloGame.Mode * Controller
with
  static member Effect(_) = Eff.marker<unit>


[<Struct>]
type GameRankingEffect =
  | SelectAll
  | InsertSelect of System.Guid * SoloGame.GameMode * Ranking.Data
with
  static member Effect(_) = Eff.marker<unit>

// type AsyncEffect<'msg> = AsyncEffect of Async<'msg>
// with
//   static member Effect(_) = Eff.marker<unit>

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