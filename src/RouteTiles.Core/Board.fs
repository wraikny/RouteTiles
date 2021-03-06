module RouteTiles.Core.Board

open RouteTiles.Common
open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.Board

open Affogato

open EffFs
open EffFs.Library

module TileDir =
  let singleTiles = [|
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
    Random.int 0 singleTiles.Length
    |> Random.map(fun d -> singleTiles.[d])

  let private singlePairs = [|
      TileDir.UpRight, (Dir.Up, Dir.Right)
      TileDir.UpDown, (Dir.Up, Dir.Down)
      TileDir.UpLeft, (Dir.Up, Dir.Left)
      TileDir.RightDown, (Dir.Right, Dir.Down)
      TileDir.RightLeft, (Dir.Right, Dir.Left)
      TileDir.DownLeft, (Dir.Down, Dir.Left)
    |]

  let private singleCorrespondence = dict singlePairs

  let correspondence =
    let pairs = [|
      yield! singlePairs
      TileDir.Cross, (Dir.Up, Dir.Down)
      TileDir.Cross, (Dir.Right, Dir.Left)
    |]
    seq {
      for d, (a, b) in pairs do
        yield ((a, d), b)
        yield ((b, d), a)
    } |> dict

  // let contains dir tileDir = correspondence.ContainsKey((dir, tileDir))

  (*
    goThrough (Dir.rev dir) -> Dir.toVector -> tryGet x y -> map ...
  *)
  let goThrough (from: Dir) (tile: TileDir) =
    correspondence.TryGetValue ((from, tile))
    |> function
    | true, x -> ValueSome x
    | _ -> ValueNone

  let (|Single|_|) x =
    singleCorrespondence.TryGetValue(x)
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

  // let value = function | RouteOrLoop.Route x -> x | RouteOrLoop.Loop x -> x

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

  [<RequireQualifiedAccess>]
  type private RouteResult =
    | Marker of (int Vector2 * int<TileId>) list
    | Self of (int Vector2 * int<TileId>) list
    | Fail of int

  let routeTiles (board: Model) =
    // タイルを辿ってその結果を返す。
    let routes firstTile cdn firstDir: RouteResult =
      let rec f tiles cdn dir =
        let cdn = cdn + Dir.toVector dir
        let rDir = Dir.rev dir
        board
        |> tryGetTile cdn
        |> function
        // Marker
        | ValueNone when board.markers |> Seq.contains (cdn.x, cdn.y, rDir) ->
          RouteResult.Marker tiles

        // タイル
        | ValueSome (ValueSome tile) ->
          TileDir.goThrough rDir tile.dir
          |> function
          | ValueSome nextDir ->
            if tile.id = firstTile.id && nextDir = firstDir then
              RouteResult.Self tiles
            else
              f ((cdn, tile.id) :: tiles) cdn nextDir
          // 接続されていない
          | ValueNone -> RouteResult.Fail tiles.Length

        // 空白
        | _ -> RouteResult.Fail tiles.Length

      f [cdn, firstTile.id] cdn firstDir

    let routesDirs id cdn dir1 dir2: LineState * RouteOrLoop voption =
      (routes id cdn dir1, routes id cdn dir2) |> function
      | RouteResult.Self tiles, RouteResult.Self _ ->
        LineState.Looped,
        Array.ofSeq tiles |> RouteOrLoop.Loop |> ValueSome
      
      | RouteResult.Marker tiles1, RouteResult.Marker tiles2 ->
        LineState.Routed,
        [| yield! tiles1; yield! tiles2 |> Seq.rev |> Seq.tail |] |> RouteOrLoop.Route |> ValueSome
      
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
          let state, tiles = getRouteState (Vector2.init x y) tile
          tiles |> Seq.iter (ValueOption.iter routesAndLoops.Add)
          { tile with routeState = state }
        )
      ), (
        routesAndLoops
        |> Seq.distinctBy(function
          | RouteOrLoop.Route tiles -> tiles |> Array.map snd |> Set.ofArray
          | RouteOrLoop.Loop tiles -> tiles |> Array.map snd |> Set.ofArray
        )
        |> Set.ofSeq
      )

    let routesAndLoopsResult =
      if routesAndLoops.IsEmpty then
        ValueNone
      else
        ValueSome(routesAndLoops, PointCalculation.calculate routesAndLoops)

    { board with tiles = tiles; routesAndLoops = routesAndLoopsResult }

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
      |> nextDirsToTiles (tiles.Length + TileDir.singleTiles.Length)
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

      let! tiles, nextTiles = eff {
        let! tiles =
          TileDir.random
          |> Random.array (size.x * size.y)

        let! nextTiles1 =
          TileDir.singleTiles
          |> Random.shuffleArray

        let! nextTiles2 =
          TileDir.singleTiles
          |> Random.shuffleArray

        return tiles, (nextTiles1, nextTiles2)
      }

      let tiles, nextTiles = initTiles tiles nextTiles size

      let board =
        {
          config = config
          markers = makeMarkers config.size

          nextId = (tiles.Length + TileDir.singleTiles.Length * 2) * 1<TileId>
          cursor = Vector.zero
          tiles = tiles
          nextTiles = nextTiles
          point = 0

          slideCount = 0

          routesAndLoops = ValueNone
          routesHistory = List.empty
          loopsHistory = List.empty

          vanishedTilesCount = 0
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

  /// タイルを移動する
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
        // ボードの外
        | ValueNone -> true
        // タイルがない
        | ValueSome ValueNone -> false
        // タイルがある
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
    let routesAndLoops, newPoint = board.routesAndLoops.Value

    let extractRouteOrLoopBy f = routesAndLoops |> Seq.filterMapV f |> Seq.map (Array.map snd) |> Seq.toList

    let vanishedRoutes = extractRouteOrLoopBy RouteOrLoop.getRoute
    let vanishedLoops = extractRouteOrLoopBy RouteOrLoop.getLoop

    let vanishedCount =
      seq {
        for route in vanishedRoutes do yield! route
        for loop in vanishedLoops do yield! loop
      }
      |> Seq.distinct
      |> Seq.length

    { board with
        point = board.point + newPoint
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
        routesAndLoops = ValueNone

        routesHistory = board.routesHistory |> List.append vanishedRoutes
        loopsHistory = board.loopsHistory |> List.append vanishedLoops
        vanishedTilesCount = board.vanishedTilesCount + vanishedCount
    }

  let inline update (msg: Msg) (board: Model) =
    let config = board.config

    msg |> function
    | ApplyVanishment ->
      vanish board |> Eff.pure'

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
                TileDir.singleTiles
                |> Random.shuffleArray

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
