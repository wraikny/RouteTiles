namespace RouteTiles.Core.Effects

open RouteTiles.Core

open EffFs

[<Struct>]
type 'a RandomEffect = RandomEffect of Random.Generator<'a>
with
  static member Effect(_: 'a RandomEffect) = Eff.output<'a>
