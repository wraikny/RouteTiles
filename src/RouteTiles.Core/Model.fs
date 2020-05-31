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
  | NoDir
  | CrossDir

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
    TileDir.UpRight, struct (Dir.Up, Dir.Right)
    TileDir.UpDown, struct (Dir.Up, Dir.Down)
    TileDir.UpLeft, struct (Dir.Up, Dir.Left)
    TileDir.RightDown, struct (Dir.Right, Dir.Down)
    TileDir.RightLeft, struct (Dir.Right, Dir.Left)
    TileDir.DownLeft, struct (Dir.Down, Dir.Left)
  |]

  let pairMap = dict pairs

  let tiles = pairs |> Array.map fst

let (|TileDir|) x = TileDir.pairMap.Item(x)

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
