namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.Board
open RouteTiles.Core.Board
open System
open System.Collections.Generic
open System.Threading.Tasks
open Affogato
open Altseed2

type internal NextTilesNode(addCoroutine) =
  inherit TransformNode(Scale = Vector2F(1.0f, 1.0f) * Consts.Board.nextsScale)

  let updateTile = BoardHelper.updateTile >> addCoroutine

  let nextsPool =
    { new NodePool<int<TileId>, _, _>() with
        member __.Create() = BoardHelper.createTile()
        member __.Update(node, (index: int, tile: Tile), isNewTile) =
          updateTile(node, (Helper.Board.nextsIndexToCoordinate index, tile), isNewTile)
    }

  let nextsBackground =
    RectangleNode(
      Color = Consts.Board.backGroundColor,
      RectangleSize = Helper.Board.nextsViewSize,
      ZOrder = ZOrder.Board.background
    )

  do
    base.AddChildNode(nextsBackground)
    base.AddChildNode(nextsPool)

  
  member __.OnNext(board) =
    nextsPool.Update(
      seq {
        let n1, n2 = board.nextTiles
        yield! n1
        yield! n2
      }
      |> Seq.indexed
      |> Seq.map(fun (i, t) -> (t.id, (i, t)))
      |> Seq.take Consts.Board.nextsCountToShow
    )

  member __.Clear() =
    nextsPool.Clear()
