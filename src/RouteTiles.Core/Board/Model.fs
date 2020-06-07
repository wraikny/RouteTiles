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
type ColorMode =
  | Default
  | Route

[<Struct>]
type Tile = {
  id: int<TileId>
  dir: TileDir
  colorMode: ColorMode
}

type BoardConfig = {
  size: int Vector2
  nextCounts: int
}

type Board = {
  config: BoardConfig
  nextId: int<TileId>
  cursor: int Vector2
  tiles: Tile voption [,]
  nextTiles: Tile list * Tile list
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
  |]

  let random =
    Random.int 0 primitiveTiles.Length
    |> Random.map(fun d -> primitiveTiles.[d])

  let private correspondence =
    let pairs = [|
      TileDir.UpRight, (Dir.Up, Dir.Right)
      TileDir.UpDown, (Dir.Up, Dir.Down)
      TileDir.UpLeft, (Dir.Up, Dir.Left)
      TileDir.RightDown, (Dir.Right, Dir.Down)
      TileDir.RightLeft, (Dir.Right, Dir.Left)
      TileDir.DownLeft, (Dir.Down, Dir.Left)
      TileDir.Cross, (Dir.Up, Dir.Down)
      TileDir.Cross, (Dir.Right, Dir.Left)
    |]
    seq {
      for d, (a, b) in pairs do
        yield ((a, d), b)
        yield ((b, d), a)
    } |> dict

  let contains dir tileDir = correspondence.ContainsKey((dir, tileDir))

  let goThrough (from: Dir) (tile: TileDir) =
    correspondence.TryGetValue ((from, tile))
    |> function
    | true, x -> ValueSome x
    | _ -> ValueNone

module Tile =
  let inline dir x = x.dir

module Board =
  let inline isOutOfBoard (cdn: int Vector2) ({ config = { size = size } }: Board) =
    cdn.x < 0 || size.x <= cdn.x || cdn.y < 0 || size.y <= cdn.y

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

  let colorize board =
    let tiles =
      board.tiles
      |> Array2D.mapi (fun x y ->
        ValueOption.map(fun tile ->
          let isRoute =
            Dir.dirs
            |> Seq.filter(fun d -> TileDir.contains d tile.dir)
            |> Seq.exists(fun d ->
              let dv = Dir.toVector d
              board.tiles
              |> Array2D.tryGet (dv.x + x) (dv.y + y)
              |> ValueOption.flatten
              |> ValueOption.bind(Tile.dir >> TileDir.goThrough (Dir.rev d))
              |> ValueOption.isSome
            )
          { tile with colorMode = if isRoute then ColorMode.Route else ColorMode.Default }
        )
      )

    { board with tiles = tiles }

  let nextDirsToTiles offset =
    Seq.mapi (fun i d ->
      { id = (offset + i) * 1<TileId>
        dir = d
        colorMode = ColorMode.Default
      }
    ) >> Seq.toList

  let initTiles tiles (nextTiles1, nextTiles2) (size: int Vector2) =
    let tiles =
      tiles
      |> Seq.mapi (fun i d ->
        { id = i * 1<TileId>
          dir = d
          colorMode = ColorMode.Default
        } |> ValueSome
      )
      |> Seq.chunkBySize size.y
      |> array2D

    let nextTilesPair =
      nextTiles1 |> nextDirsToTiles tiles.Length,
      nextTiles2 |> nextDirsToTiles (tiles.Length + TileDir.primitiveTiles.Length)

    tiles, nextTilesPair

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
        { nextId = (tiles.Length + TileDir.primitiveTiles.Length * 2) * 1<TileId>
          config = config
          cursor = Vector.zero
          tiles = tiles
          nextTiles = nextTiles
        }
        |> colorize

      return board
    }
