namespace RouteTiles.Core.Board

open RouteTiles.Core
open RouteTiles.Core.Board.Model

type Msg = SlideLane of int

module Update =
  let slideLane (lane: int) (nextDir: TileDir) (board: Board): Board =
    if lane < 0 || board.config.size.x <= lane then failwithf "lane = '%d' is out of range" lane
    else
      let newTile =
        { id = board.nextId
          dir = nextDir
          colorMode = ColorMode.Default
        }

      let (nextTiles, next) = board.nextTiles |> Array.pushFrontPopBack newTile
      let tiles =
        board.tiles
        |> Array.mapOfIndex lane (Array.pushFrontPopBack next >> fst)

      { board with
          nextId = board.nextId + 1<TileId>
          tiles = tiles
          nextTiles = nextTiles
      }
      |> Board.colorize


  open EffFs
  open RouteTiles.Core.Effects

  let inline update (msg: Msg) (board: Board) =
    msg |> function
    | SlideLane lane ->
      eff {
        let nextsHeadDir = board.nextTiles |> Array.head |> Tile.dir

        let! nextDir =
          Random.int 0 6
          |> Random.map(fun i -> TileDir.primitiveTiles.[i])
          |> Random.until((<>) nextsHeadDir)
          |> RandomEffect

        let board = slideLane lane nextDir board
        return board
      }
