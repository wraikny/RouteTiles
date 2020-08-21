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
type GameControlEffect =
  | Pause
  | Resume
  | Restart
  | Quit
with
  static member Effect(_) = Eff.output<unit>

[<Struct>]
type LogEffect = LogEffect of string
with
  static member Effect(_: LogEffect) = Eff.output<unit>

type CurrentControllers = CurrentControllers with
  static member Effect(_) = Eff.output<Controller[]>


[<RequireQualifiedAccess>]
type SoundEffect =
  | Select
  | Move
  | Invalid
with
  static member Effect(_) = Eff.output<unit>

type GameStartEffect = GameStartEffect of SoloGame.Mode * Controller with
  static member Effect(_) = Eff.output<unit>

[<AutoOpen>]
module Utils =
  let logEffect format = sprintf format >> LogEffect

