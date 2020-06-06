namespace RouteTiles.Core.Board

open RouteTiles.Core
open RouteTiles.Core.Board.Model

open Affogato

type Msg =
  | MoveCursor of Dir
  | Slide of Dir

module Update =
  let sldieTiles (slideDir: Dir) (nextDir: TileDir) (board: Board): Board =
    let newTile =
      { id = board.nextId
        dir = nextDir
        colorMode = ColorMode.Default
      }

    let (nextTiles, next) = board.nextTiles |> Array.pushFrontPopBack newTile

    let tiles =
      let dirVec = Dir.toVector slideDir

      let isSlideTarget =
        slideDir |> function
        | Dir.Up | Dir.Down -> fun x _ -> x = board.cursor.x
        | Dir.Right | Dir.Left -> fun _ y -> y = board.cursor.y

      board.tiles
      |> Array2D.mapi(fun x y tile ->
        if isSlideTarget x y then
          let cdn = Vector2.init x y - dirVec
          board.tiles
          |> Array2D.tryGet cdn.x cdn.y
          |> ValueOption.defaultValue next
        else
          tile
      )

    { board with
        nextId = board.nextId + 1<TileId>
        tiles = tiles
        nextTiles = nextTiles
    }
    |> Board.colorize


  open EffFs
  open RouteTiles.Core.Effects

  let inline update (msg: Msg) (board: Board) =
    let config = board.config

    msg |> function
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
        let nextsHeadDir = board.nextTiles |> Array.head |> Tile.dir

        let! nextDir =
          Random.int 0 6
          |> Random.map(fun i -> TileDir.primitiveTiles.[i])
          |> Random.until((<>) nextsHeadDir)
          |> RandomEffect

        let board = sldieTiles dir nextDir board
        return board
      }
