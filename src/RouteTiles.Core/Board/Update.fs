namespace RouteTiles.Core.Board

open RouteTiles.Core
open RouteTiles.Core.Board.Model

open Affogato

(*

Msg.Slide
-> Update (colorized)
-> (Waiting Animation -> Apply Color)
-> Msg.CheckVanishment
-> ? (View: Vanishment Animation)
-> ...

*)
[<Struct>]
type Msg =
  | MoveCursor of moveCursor:Dir
  | Slide of slide:Dir
  | ApplyVanishment

module Update =
  let calculatePoint (routesAndLoops: Set<RouteOrLoop>) =
    routesAndLoops
    |> Seq.sumBy(function
      | RouteOrLoop.Route tiles -> tiles.Count * tiles.Count * 3
      | RouteOrLoop.Loop tiles -> tiles.Count * tiles.Count * 2
    )
    |> float32
    |> ( * ) (1.0f + float32 routesAndLoops.Count / 5.0f)
    |> int

  let sldieTiles (slideDir: Dir) (nextTile) (board: Board): Board =
    let tiles =
      let dirVec = Dir.toVector slideDir

      let isSlideTarget =
        slideDir |> function
        | Dir.Up | Dir.Down -> fun x _ -> x = board.cursor.x
        | Dir.Right | Dir.Left -> fun _ y -> y = board.cursor.y

      let rec isSlidedTile cdn =
        board
        |> Board.tryGetTile cdn
        |> function
        | ValueNone -> true
        | ValueSome ValueNone -> false
        | _ -> isSlidedTile (cdn - dirVec)

      board.tiles
      |> Array2D.mapi(fun x y tile ->
        let cdn = Vector2.init x y - dirVec
        if isSlideTarget x y && isSlidedTile cdn then
          board.tiles
          |> Array2D.tryGet cdn.x cdn.y
          |> function
          | ValueNone -> ValueSome nextTile
          | ValueSome ValueNone -> failwith "Unecpected pattern, already excluded by 'isSlidedTile'"
          | ValueSome x -> x
        else
          tile
      )

    { board with
        nextId = board.nextId + 1<TileId>
        tiles = tiles
    }
    |> Board.routeTiles

  let vanish board =
    let routesAndLoops = board.routesAndLoops

    let extractAndAppend f =
      List.append(routesAndLoops |> Seq.filterMapV f |> Seq.map (Set.map snd) |> Seq.toList)

    { board with
        point = board.point + calculatePoint routesAndLoops
        tiles =
          board.tiles
          |> Array2D.map(function
            | ValueSome { routeState = RouteState.Single(LineState.Routed) }
            | ValueSome { routeState = RouteState.Single(LineState.Looped) }
            | ValueSome { routeState = RouteState.Cross(LineState.Routed, _) }
            | ValueSome { routeState = RouteState.Cross(LineState.Looped, _) }
            | ValueSome { routeState = RouteState.Cross(_, LineState.Routed) }
            | ValueSome { routeState = RouteState.Cross(_, LineState.Looped) }
            | ValueNone -> ValueNone
            | x -> x
          )
        routesAndLoops = Set.empty

        routesHistory = board.routesHistory |> extractAndAppend RouteOrLoop.getRoute
        loopsHistory = board.routesHistory |> extractAndAppend RouteOrLoop.getLoop
    }

  open EffFs
  open RouteTiles.Core.Effects

  let inline update (msg: Msg) (board: Board) =
    let config = board.config

    msg |> function
    | ApplyVanishment ->
      vanish board
      |> Eff.pure'

    | MoveCursor dir ->
      let cursor = board.cursor + Dir.toVector dir

      let cursor =
        Vector2.init
          (cursor.x |> clamp 0 (config.size.x-1))
          (cursor.y |> clamp 0 (config.size.y-1))

      if cursor = board.cursor then board
      else { board with cursor = cursor }
      |> Eff.pure'

    | Slide dir ->
      eff {
        let! (board, next) =
          match board.nextTiles with
          | next::nexts, nexts2 ->
            ({ board with nextTiles = (nexts, nexts2)}, next)
            |> Eff.pure'
          | [], next::nexts ->
            eff {
              let! newNexts =
                TileDir.primitiveTiles
                |> Random.shuffle
                |> RandomEffect

              let newNexts =
                newNexts
                |> Board.nextDirsToTiles (int board.nextId)
                |> Seq.toList

              let board =
                { board with
                    nextTiles = (nexts, newNexts)
                    nextId = board.nextId + newNexts.Length * 1<TileId>
                }

              return board, next
            }
          | x -> failwithf "Unexpected nextTiles state: %A" x

        let board = sldieTiles dir next board
        return board
      }
