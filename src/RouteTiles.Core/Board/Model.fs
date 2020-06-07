namespace RouteTiles.Core.Board.Model

open RouteTiles.Core
open RouteTiles.Core.Effects

open Affogato

[<Measure>]type TileId

[<Struct; RequireQualifiedAccess>]
type TileDir =
  | UpRight
  | UpDown
  | UpLeft
  | RightDown
  | RightLeft
  | DownLeft
  | Cross
  | Empty

[<Struct; RequireQualifiedAccess>]
type LineState =
  | Default
  | Routing
  | Looping
  | Routed
  | Looped

[<Struct; RequireQualifiedAccess>]
type RouteState =
  | Single of single:LineState
  | Cross of horizontal:LineState * vertical:LineState
  | Empty

[<RequireQualifiedAccess>]
type RouteOrLoop =
  | Route of Set<int Vector2 * int<TileId>>
  | Loop of Set<int Vector2 * int<TileId>>

[<Struct>]
type Tile = {
  id: int<TileId>
  dir: TileDir
  routeState: RouteState
}

type BoardConfig = {
  size: int Vector2
  nextCounts: int
}

type Board = {
  config: BoardConfig
  markers: (int * int * Dir)[]

  nextId: int<TileId>
  cursor: int Vector2
  tiles: Tile voption [,]
  nextTiles: Tile list * Tile list

  point: int

  routesAndLoops: Set<RouteOrLoop>
  routesHistory: Set<int<TileId>> list
  loopsHistory: Set<int<TileId>> list
}

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

module Board =
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

  let inline tryGetTile (cdn: int Vector2) (board: Board) = board.tiles |> Array2D.tryGet cdn.x cdn.y

  open System.Collections.Generic

  [<RequireQualifiedAccess>]
  type private RouteResult =
    | Marker of (int Vector2 * int<TileId>) list
    | Self of (int Vector2 * int<TileId>) list
    | Fail of int

  let routeTiles (board: Board) =
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

    let routesAndLoops = ResizeArray()

    let tiles =
      board.tiles
      |> Array2D.mapi (fun x y ->
        ValueOption.map(fun tile ->
          let state, rls = getRouteState (Vector2.init x y) tile
          rls |> Seq.iter(ValueOption.iter(routesAndLoops.Add))
          { tile with routeState = state }
        )
      )

    { board with tiles = tiles; routesAndLoops = Set.ofSeq routesAndLoops }

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

          routesAndLoops = Set.empty
          routesHistory = List.empty
          loopsHistory = List.empty
        }
        |> routeTiles

      return board
    }
