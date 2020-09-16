namespace RouteTiles.Core.Effects

open RouteTiles.Core
open RouteTiles.Core.Types

open EffFs

[<Struct>]
type EmitVanishParticleEffect = EmitVanishParticleEffect of Set<Types.Board.RouteOrLoop>
with
  static member Effect(_: EmitVanishParticleEffect) = Eff.marker<unit>

[<Struct; RequireQualifiedAccess>]
type GameControlEffect =
  | Pause
  | Resume
  | Start of SoloGame.Mode * Controller
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
type GameRankingEffect<'msg> =
  GameRankingEffect of
    {|
      mode: SoloGame.Mode
      guid: System.Guid
      result: Menu.GameResult
      onSuccess: int64 * SimpleRankingsServer.Data<Menu.GameResult>[] -> 'msg
      onError: string -> 'msg
    |}
with
  static member Effect(_) = Eff.marker<unit>

// type AsyncEffect<'msg> = AsyncEffect of Async<'msg>
// with
//   static member Effect(_) = Eff.marker<unit>

[<Struct>]
type SaveConfig = SaveConfig of Config
with
  static member Effect(_) = Eff.marker<unit>
