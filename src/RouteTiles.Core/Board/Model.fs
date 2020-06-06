namespace RouteTiles.Core.Board.Model

open RouteTiles.Core
open RouteTiles.Core.Effects

open Affogato

[<Measure>]type TileId

[<Struct; RequireQualifiedAccess>]
type Dir = Up | Down | Right | Left

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
  tiles: Tile[][]
  nextTiles: Tile[]
}
open EffFs

module Dir =
  let dirs = [| Dir.Up; Dir.Down; Dir.Right; Dir.Left |]

  let inline rev dir = dir |> function
    | Dir.Up -> Dir.Down
    | Dir.Right -> Dir.Left
    | Dir.Down -> Dir.Up
    | Dir.Left -> Dir.Right

  let inline toVector (dir: Dir) =
    let (a, b) = dir |> function
      | Dir.Up -> (0, -1)
      | Dir.Down -> (0, 1)
      | Dir.Right -> (1, 0)
      | Dir.Left -> (-1, 0)

    Vector2.init a b

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

  let getTile (cdn: int Vector2) (board: Board) =
    if isOutOfBoard cdn board then ValueNone
    else ValueSome board.tiles.[cdn.x].[cdn.y]

  let getTiles board: (int Vector2 * Tile)[] =
    let size = board.config.size;
    [|
      for lane in 0..size.x-1 do
      for y in 0..size.y-1 do
        yield (Vector2.init lane y, board.tiles.[lane].[y])
    |]

  let colorize board =
    let tiles =
      getTiles board
      |> Seq.map (fun (cdn, tile) ->
        let isRoute =
          Dir.dirs
          |> Seq.filter(fun d -> TileDir.contains d tile.dir)
          |> Seq.exists(fun d ->
            board
            |> getTile (cdn + Dir.toVector d)
            |> ValueOption.bind(Tile.dir >> TileDir.goThrough (Dir.rev d))
            |> ValueOption.isSome
          )
        { tile with colorMode = if isRoute then ColorMode.Route else ColorMode.Default }
      )
      |> Seq.chunkBySize board.config.size.y
      |> Array.ofSeq

    { board with tiles = tiles }

  let initTiles tiles nextTiles (size: int Vector2) =
    let tiles =
      tiles
      |> Seq.mapi (fun i d ->
        { id = i * 1<TileId>
          dir = d
          colorMode = ColorMode.Default
        }
      )
      |> Seq.chunkBySize size.y
      |> Seq.toArray

    let nextTiles =
      nextTiles
      |> Seq.mapi (fun i d ->
        { id = (tiles.Length * size.y + i) * 1<TileId>
          dir = d
          colorMode = ColorMode.Default
        }
      )
      |> Seq.toArray

    tiles, nextTiles

  let inline init config =
    eff {
      let { nextCounts = nextCounts; size = size } = config

      let! tiles, nextTiles = RandomEffect(random {
        let! tiles =
          TileDir.random
          |> Random.seq (size.x * size.y)

        let! nextTiles =
          TileDir.random
          |> Random.seq nextCounts

        return tiles, nextTiles
      })

      let tiles, nextTiles = initTiles tiles nextTiles size

      let board =
        { nextId = (size.x * size.y + nextCounts) * 1<TileId>
          config = config
          tiles = tiles
          nextTiles = nextTiles
        }
        |> colorize

      return board
    }
