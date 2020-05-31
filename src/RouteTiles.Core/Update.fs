module RouteTiles.Core.Update

open Utils
open Model

module TileDir =
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

  let goThrough (from: Dir) (tile: TileDir) =
    correspondence.TryGetValue ((from, tile))
    |> function
    | true, x -> ValueSome x
    | _ -> ValueNone

module Board =
  let slideLane (lane: int) (nextDir: TileDir) (board: Board): Board voption =
    if lane < 0 || board.size.x <= lane then ValueNone
    else
      let newTile = { id = board.nextId; dir = nextDir }
      let (nextTiles, next) = board.nextTiles |> Array.pushFrontPopBack newTile
      let tiles =
        board.tiles
        |> Array.mapOfIndex lane (Array.pushFrontPopBack next >> fst)

      ValueSome
        { board with
            nextId = board.nextId + 1<TileId>
            tiles = tiles
            nextTiles = nextTiles
        }


type GameMsg =
  | SlideLane of int

open EffFs
open Effect

module Game =
  let inline update (msg: GameMsg) (game: Game) =
    msg |> function
    | SlideLane lane ->
      eff {
        let! nextIndex = RandomInt(0, 5)
        let nextDir = TileDir.primitiveTiles.[nextIndex]
        let board = Board.slideLane lane nextDir game.board |> ValueOption.get
        let game = { game with board = board }
        return game
      }
