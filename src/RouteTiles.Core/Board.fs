module RouteTiles.Core.Board

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.Board
open RouteTiles.Core.Effects

open Affogato

open EffFs

module TileDir =
  let primitiveTiles = [|
    TileDir.UpRight
    TileDir.UpDown
    TileDir.UpLeft
    TileDir.RightDown
    TileDir.RightLeft
    TileDir.DownLeft

    // temp
    TileDir.Cross
  |]

  let random =
    Random.int 0 primitiveTiles.Length
    |> Random.map(fun d -> primitiveTiles.[d])

  let private primitivePairs = [|
      TileDir.UpRight, (Dir.Up, Dir.Right)
      TileDir.UpDown, (Dir.Up, Dir.Down)
      TileDir.UpLeft, (Dir.Up, Dir.Left)
      TileDir.RightDown, (Dir.Right, Dir.Down)
      TileDir.RightLeft, (Dir.Right, Dir.Left)
      TileDir.DownLeft, (Dir.Down, Dir.Left)
    |]

  let private primitiveCorrespondence = dict primitivePairs

  let correspondence =
    let pairs = [|
      yield! primitivePairs
      TileDir.Cross, (Dir.Up, Dir.Down)
      TileDir.Cross, (Dir.Right, Dir.Left)
    |]
    seq {
      for d, (a, b) in pairs do
        yield ((a, d), b)
        yield ((b, d), a)
    } |> dict

  let contains dir tileDir = correspondence.ContainsKey((dir, tileDir))

  (*
    goThrough (Dir.rev dir) -> Dir.toVector -> tryGet x y -> map ...
  *)
  let goThrough (from: Dir) (tile: TileDir) =
    correspondence.TryGetValue ((from, tile))
    |> function
    | true, x -> ValueSome x
    | _ -> ValueNone

  let (|Single|_|) x =
    primitiveCorrespondence.TryGetValue(x)
    |> function
    | true, res -> Some(Single(res))
    | _ -> None

module RouteState =
  let defaultFrom = function
    | TileDir.Empty -> RouteState.Empty
    | TileDir.Cross -> RouteState.Cross(LineState.Default, LineState.Default)
    | _ -> RouteState.Single LineState.Default

module RouteOrLoop =
  let getRoute = function | RouteOrLoop.Route x -> ValueSome x | _ -> ValueNone
  let getLoop = function | RouteOrLoop.Loop x -> ValueSome x | _ -> ValueNone

module Tile =
  let inline dir x = x.dir

module Model =
  let getTiles board: (int Vector2 * Tile)[] =
    let size = board.config.size;
    [|
      for y in 0..size.y-1 do
      for x in 0..size.x-1 do
        match board.tiles.[x, y] with
        | ValueSome t ->
          yield (Vector2.init x y, t)
        | _ -> ()
    |]

  let inline tryGetTile (cdn: int Vector2) (board: Model) = board.tiles |> Array2D.tryGet cdn.x cdn.y

  open System.Collections.Generic

  [<RequireQualifiedAccess>]
  type private RouteResult =
    | Marker of (int Vector2 * int<TileId>) list
    | Self of (int Vector2 * int<TileId>) list
    | Fail of int

  let routeTiles (board: Model) =
    let routes firstTile cdn firstDir =
      let rec f tiles cdn dir =
        let cdn = cdn + Dir.toVector dir
        let rDir = Dir.rev dir
        board
        |> tryGetTile cdn
        |> function
        | ValueNone when board.markers |> Seq.contains (cdn.x, cdn.y, rDir) -> RouteResult.Marker tiles
        | ValueSome (ValueSome tile) ->
          TileDir.goThrough rDir tile.dir
          |> function
          | ValueSome nextDir ->
            if tile.id = firstTile.id && nextDir = firstDir then
              RouteResult.Self tiles
            else
              f ((cdn, tile.id) :: tiles) cdn nextDir
          | ValueNone -> RouteResult.Fail tiles.Length
        | _ -> RouteResult.Fail tiles.Length

      f [cdn, firstTile.id] cdn firstDir

    let routesDirs id cdn dir1 dir2 =
      (routes id cdn dir1, routes id cdn dir2) |> function
      | RouteResult.Self tiles, RouteResult.Self _ ->
        LineState.Looped,
        Set.ofSeq tiles |> RouteOrLoop.Loop |> ValueSome
      
      | RouteResult.Marker tiles1, RouteResult.Marker tiles2 ->
        LineState.Routed,
        Set.ofSeq(seq { yield! tiles1; yield! tiles2 }) |> RouteOrLoop.Route |> ValueSome
      
      | RouteResult.Marker _, _ | _, RouteResult.Marker _ -> LineState.Routing, ValueNone
      | RouteResult.Fail a, RouteResult.Fail b when a + b > 2 -> LineState.Looping, ValueNone
      | _ -> LineState.Default, ValueNone


    let getRouteState cdn (tile: Tile) =
      match tile.dir with
      | TileDir.Empty -> RouteState.Empty, Seq.empty
      | TileDir.Cross ->
        let hState, hrl = routesDirs tile cdn Dir.Right Dir.Left
        let vState, vrl = routesDirs tile cdn Dir.Up Dir.Down
        RouteState.Cross(hState, vState), seq { yield vrl; yield hrl }
      | TileDir.Single(a, b) ->
        let state, rl = routesDirs tile cdn a b
        RouteState.Single state, seq { yield rl }
      | x -> failwithf "Unexpected pattern: %A" x


    let tiles, routesAndLoops =
      // todo
      let routesAndLoops = ResizeArray()

      board.tiles
      |> Array2D.mapi (fun x y ->
        ValueOption.map(fun tile ->
          let state, rls = getRouteState (Vector2.init x y) tile
          rls |> Seq.iter(ValueOption.iter(routesAndLoops.Add))
          { tile with routeState = state }
        )
      ), Set.ofSeq routesAndLoops

    { board with tiles = tiles; routesAndLoops = routesAndLoops }

  let nextDirsToTiles offset =
    Seq.mapi (fun i d ->
      { id = (offset + i) * 1<TileId>
        dir = d
        routeState = RouteState.defaultFrom d
      }
    )

  let initTiles tiles (nextTiles1, nextTiles2) (size: int Vector2) =
    let tiles =
      tiles
      |> nextDirsToTiles 0
      |> Seq.map ValueSome
      |> Seq.chunkBySize size.y
      |> array2D

    let nextTilesPair =
      nextTiles1
      |> nextDirsToTiles tiles.Length
      |> Seq.toList,
      nextTiles2
      |> nextDirsToTiles (tiles.Length + TileDir.primitiveTiles.Length)
      |> Seq.toList

    tiles, nextTilesPair


  let makeMarkers (size: int Vector2) =
    [|
      for y in 1..3 do
        yield (-1, y, Dir.Right)
        yield (size.x, y, Dir.Left)

      for x in 1..2 do
        yield (x, -1, Dir.Down)
        yield (x, size.y, Dir.Up)
    |]


  let inline init config =
    eff {
      let { nextCounts = _; size = size } = config

      let! tiles, nextTiles = RandomEffect(random {
        let! tiles =
          TileDir.random
          |> Random.seq (size.x * size.y)

        let! nextTiles1 =
          TileDir.primitiveTiles
          |> Random.shuffle

        let! nextTiles2 =
          TileDir.primitiveTiles
          |> Random.shuffle

        return tiles, (nextTiles1, nextTiles2)
      })

      let tiles, nextTiles = initTiles tiles nextTiles size

      let board =
        {
          config = config
          markers = makeMarkers config.size

          nextId = (tiles.Length + TileDir.primitiveTiles.Length * 2) * 1<TileId>
          cursor = Vector.zero
          tiles = tiles
          nextTiles = nextTiles
          point = 0

          slideCount = 0

          routesAndLoops = Set.empty
          routesHistory = List.empty
          loopsHistory = List.empty
        }
        |> routeTiles

      return board
    }

[<Struct>]
type Msg =
  | MoveCursor of moveCursor:Dir
  | Slide of slide:Dir
  | ApplyVanishment

module Update =
  let incr model = { model with slideCount = model.slideCount + 1 }

  let calculatePoint (routesAndLoops: Set<RouteOrLoop>) =
    routesAndLoops
    |> Seq.sumBy(function
      | RouteOrLoop.Route tiles -> tiles.Count * tiles.Count * 3
      | RouteOrLoop.Loop tiles -> tiles.Count * tiles.Count * 2
    )
    |> float32
    |> ( * ) (1.0f + float32 routesAndLoops.Count / 5.0f)
    |> int

  let sldieTiles (slideDir: Dir) (nextTile) (board: Model): Model =
    let tiles =
      let dirVec = Dir.toVector slideDir

      let isSlideTarget =
        slideDir |> function
        | Dir.Up | Dir.Down -> fun x _ -> x = board.cursor.x
        | Dir.Right | Dir.Left -> fun _ y -> y = board.cursor.y

      let rec isSlidedTile cdn =
        board
        |> Model.tryGetTile cdn
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
    |> Model.routeTiles

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

  let inline update (msg: Msg) (board: Model) =
    let config = board.config

    msg |> function
    | ApplyVanishment ->
      eff {
        if not board.routesAndLoops.IsEmpty then
          do! EmitVanishParticleEffect board.routesAndLoops

        return vanish board
      }

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
                |> Model.nextDirsToTiles (int board.nextId)
                |> Seq.toList

              let board =
                { board with
                    nextTiles = (nexts, newNexts)
                    nextId = board.nextId + newNexts.Length * 1<TileId>
                }

              return board, next
            }
          | x -> failwithf "Unexpected nextTiles state: %A" x

        let board = board |> sldieTiles dir next |> incr
        return board
      }
