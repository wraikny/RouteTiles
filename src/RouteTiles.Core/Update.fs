module RouteTiles.Core.Update

open Utils
open Model

// module TileDir =
  

module Board =
  let slideLane (lane: int) (nextDir: TileDir) (board: Board): Board voption =
    if lane < 0 || board.size.x <= lane then ValueNone
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
      |> ValueSome


type GameMsg =
  | SlideLane of int

open EffFs
open Effect

module Game =
  let inline update (msg: GameMsg) (game: Game) =
    msg |> function
    | SlideLane lane ->
      eff {
        let nextsHeadDir = game.board.nextTiles |> Array.head |> Tile.dir

        let! nextDir =
          Random.int 0 6
          |> Random.map(fun i -> TileDir.primitiveTiles.[i])
          |> Random.until((<>) nextsHeadDir)
          |> RandomEffect

        let board = Board.slideLane lane nextDir game.board |> ValueOption.get
        let game = { game with board = board }
        return game
      }
