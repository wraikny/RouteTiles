module RouteTiles.Core.Model

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

[<Struct>]
type Tile = {
  id: int<TileId>
  dir: TileDir
}

type Board = {
  size: int Vector2
  nextId: int<TileId>
  tiles: Tile[][]
  nextTiles: Tile[]
}

type Game = {
  board: Board
  points: int
}

open Effect
open EffFs

module Dir =
  let toVector (dir: Dir) =
    let (a, b) = dir |> function
      | Dir.Up -> (0, -1)
      | Dir.Down -> (0, 1)
      | Dir.Right -> (1, 0)
      | Dir.Left -> (-1, 0)

    Vector2.init a b

module TileDir =
  let pairs = [|
    TileDir.UpRight, (Dir.Up, Dir.Right)
    TileDir.UpDown, (Dir.Up, Dir.Down)
    TileDir.UpLeft, (Dir.Up, Dir.Left)
    TileDir.RightDown, (Dir.Right, Dir.Down)
    TileDir.RightLeft, (Dir.Right, Dir.Left)
    TileDir.DownLeft, (Dir.Down, Dir.Left)
  |]

  // let pairMap = dict pairs

  let primitiveTiles = pairs |> Array.map fst

module Board =
  let inline isOutOfBoard (cdn: int Vector2) (board: Board) =
    cdn.x < 0 || board.size.x <= cdn.x || cdn.y < 0 || board.size.y <= cdn.y

  let getTile (cdn: int Vector2) (board: Board) =
    if isOutOfBoard cdn board
    then ValueNone
    else ValueSome board.tiles.[cdn.x].[cdn.y]

  let getTiles board: (Tile * int Vector2)[] =
    [|
      for lane in 0..board.size.x-1 do
      for y in 0..board.size.y-1 do
        yield (board.tiles.[lane].[y], Vector2.init lane y)
    |]

  let inline init (nextCounts: int) (size: int Vector2) =
    eff {
      let! tiles = RandomIntArray(size.x * size.y, (0, 6))
      let tiles =
        tiles
        |> Array.mapi(fun i d -> { id = i * 1<TileId>; dir = TileDir.primitiveTiles.[d]})
        |> Array.chunkBySize size.y

      let! nextTiles = RandomIntArray(nextCounts, (0, 6))
      let nextTiles = nextTiles |> Array.mapi(fun i d -> { id = (size.x * size.y + i) * 1<TileId>; dir = TileDir.primitiveTiles.[d]})

      return {
        nextId = (size.x * size.y + nextCounts) * 1<TileId>
        size = size
        tiles = tiles
        nextTiles = nextTiles
      }
    }

module Game =
  let inline init nextCounts size =
    eff {
      let! board = Board.init nextCounts size
      return { board = board; points = 0 }
    }

  let inline addPoint p game = { game with points = p + game.points }
