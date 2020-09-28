module RouteTiles.Core.PointCalculation

open RouteTiles.Core.Types.Board

let calculate (routesAndLoops: Set<RouteOrLoop>) =
  // 同時消しボーナス
  let synchronousBonus = 1.0f + 2.0f * float32 routesAndLoops.Count

  // 交差ボーナス
  let crossBonus: float32 =
    let allVanishedTileIds =
      [|for rl in routesAndLoops do
          for (_, id) in rl.Value do
            yield id
      |]

    let idDistinctedLength = allVanishedTileIds |> Array.distinct |> Array.length
    
    // 交差数
    let crossCount = allVanishedTileIds.Length - idDistinctedLength

    (float32 crossCount + pown 1.5f crossCount)

  routesAndLoops
  |> Seq.sumBy(fun rl ->
    // 種類ボーナス
    let kindBonus = rl |> function
      | RouteOrLoop.Route _ -> 4.0f
      | RouteOrLoop.Loop _ -> 6.0f

    let tiles = rl.Value

    // 連結ボーナス
    let connectionBonus = pown (2 + tiles.Length) 2 |> float32

    kindBonus * connectionBonus
  )
  |> ( * ) (synchronousBonus * crossBonus)
  |> int
