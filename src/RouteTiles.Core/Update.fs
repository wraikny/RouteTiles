module RouteTiles.Core.Update

open Utils
open Model

module TileDir =
  open TileDir

  let private correspondence =
    seq {
      for d, struct (a, b) in pairs do
        yield (struct (a, d), b)
        yield (struct (b, d), a)
    } |> dict

  let goThrough (from: Dir) (tile: TileDir) =
    correspondence.TryGetValue (struct (from, tile))
    |> function
    | true, x -> ValueSome x
    | _ -> ValueNone

module Board =
  open Board

  let slideLane (lane: int) (nextDir: TileDir) (board: Board): Board voption =
    if lane < 0 || board.size.x <= lane then ValueNone
    else
      let newTile = { id = board.nextId; dir = nextDir }
      let (nextTiles, next) = board.nextTiles |> Array.pushFrontPopBack newTile
      let tiles =
        board.tiles
        |> Array.mapOfIndex lane (Array.pushFrontPopBack next.Value >> fst)

      ValueSome
        { board with
            nextId = board.nextId + 1<TileId>
            tiles = tiles
            nextTiles = nextTiles
        }
