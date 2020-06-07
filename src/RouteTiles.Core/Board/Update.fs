namespace RouteTiles.Core.Board

open RouteTiles.Core
open RouteTiles.Core.Board.Model

open Affogato

type Msg =
  | MoveCursor of Dir
  | Slide of Dir

module Update =
  let sldieTiles (slideDir: Dir) (nextTile) (board: Board): Board =
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
          |> function
          | ValueNone -> ValueSome nextTile
          | ValueSome ValueNone -> tile
          | ValueSome x -> x
        else
          tile
      )

    { board with
        nextId = board.nextId + 1<TileId>
        tiles = tiles
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
        let! (board, next) =
          match board.nextTiles with
          | next::nexts, nexts2 ->
            ({ board with nextTiles = (nexts, nexts2)}, next)
            |> Eff.pure'
          | [], next::nexts ->
            eff {
              let! newNexts =
                TileDir.primitiveTiles
                |> Random.shuffle
                |> RandomEffect

              let newNexts = newNexts |> Board.nextDirsToTiles (int board.nextId)

              let board =
                { board with
                    nextTiles = (nexts, newNexts)
                    nextId = board.nextId + newNexts.Length * 1<TileId>
                }

              return board, next
            }
          | x -> failwithf "Unexpected nextTiles state: %A" x

        let board = sldieTiles dir next board
        return board
      }
