namespace RouteTiles.Core.Effects

open RouteTiles.Core
open RouteTiles.Core.Types

open EffFs

[<Struct>]
type 'a RandomEffect = RandomEffect of Random.Generator<'a>
with
  static member Effect(_: 'a RandomEffect) = Eff.output<'a>

[<Struct>]
type EmitVanishParticleEffect = EmitVanishParticleEffect of Set<Types.Board.RouteOrLoop>
with
  static member Effect(_: EmitVanishParticleEffect) = Eff.output<unit>

[<Struct; RequireQualifiedAccess>]
type ControlEffect =
  | SetIsPaused of bool
  | Restart
  | Quit
with
  static member Effect(_: ControlEffect) = Eff.output<unit>

[<Struct>]
type LogEffect = LogEffect of string
with
  static member Effect(_: LogEffect) = Eff.output<unit>

[<AutoOpen>]
module Utils =
  let logEffect format = sprintf format >> LogEffect


type CurrentControllers = CurrentControllers with
  static member Effect(_) = Eff.output<Controller[]>

[<Struct; RequireQualifiedAccess>]
type SoundKind =
  | Select
  | Move
  | Invalid

type SoundEffect = SoundEffect of SoundKind with
  static member Effect(_) = Eff.output<unit>

type GameStartEffect = GameStartEffect of SoloGame.Mode * Controller with
  static member Effect(_) = Eff.output<unit>
