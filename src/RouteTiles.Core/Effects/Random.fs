namespace RouteTiles.Core.Effects

[<RequireQualifiedAccess>]
module Random =
  type 'a Generator = Generator of (System.Random -> 'a)

  let inline private gf (Generator f) = f

  type 'a Generator with
    static member (>>=) (x: 'a Generator, f: 'a -> 'b Generator) : 'b Generator =
      Generator(fun r -> gf (f (gf x r)) r)

    static member Return(x: 'a) = Generator(fun _ -> x)

  let inline bind (f : 'a -> 'b Generator) (x : 'a Generator) : 'b Generator =
    Generator.(>>=)(x, f)

  let bool : bool Generator =
    Generator <| fun r -> r.Next() % 2 = 0

  let int (minValue : int) (maxValue : int) : int Generator =
    Generator <| fun r -> r.Next(minValue, maxValue)

  let float : float Generator =
      Generator <| fun r -> r.NextDouble()

  let seq (length : int) (g : 'a Generator) : 'a seq Generator =
    Generator <|fun r -> seq { for _i = 1 to length do yield gf g r }

  let inline until (f: 'a -> bool) (generator : 'a Generator) : 'a Generator =
    let rec loop xs =
      if f xs then Generator.Return xs
      else generator >>= loop

    generator >>= loop

  let inline map (f : 'a -> 'b) (x : 'a Generator) : 'b Generator =
    bind (Generator.Return << f) x

  let inline map2 (f : 'a -> 'b -> 'c) (g1 : 'a Generator) (g2 : 'b Generator) : 'c Generator =
    g1 >>= fun x1 ->
    g2 >>= fun x2 ->
      f x1 x2 |> Generator.Return

  let inline pair (g1 : 'a Generator) (g2 : 'b Generator) : ('a * 'b) Generator =
    map2 (fun a b -> a, b) g1 g2

  type RandomBuilder() =
    member __.Return(x) = Generator.Return(x)
    member __.Bind(x,f) = Generator.(>>=)(x,f)

