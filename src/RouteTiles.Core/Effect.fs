module RouteTiles.Core.Effect

open EffFs

type RandomInt = RandomInt of int * int
with
  static member Effect(_) = Eff.output<int>

type RandomIntArray = RandomIntArray of int * (int * int) with
  static member Effect(_) = Eff.output<int[]>
