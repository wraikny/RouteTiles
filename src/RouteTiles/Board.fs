namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Model
open RouteTiles.Core.Utils
open System
open System.Threading.Tasks
open Affogato
open Altseed

type Board() =
  inherit Node()

  let coroutineNode = CoroutineNode()

  let createTile() =
    let node =
      SpriteNode(
        Texture = Texture2D.Load(@"tiles.png"),
        Src = RectF(0.0f, 0.0f, 100.0f, 100.0f),
        ZOrder = ZOrder.board 1
      )

    node.AdjustSize()
    node.CenterPosition <- Consts.tileSize / 2.0f

    node

  let updateTile (node: SpriteNode, (cdn, tile), isNewTile) =
    let pos = Helper.calcTilePosCenter cdn
    let src = Binding.tileTextureSrc tile

    if isNewTile then
      node.IsDrawn <- false
      node.Position <- pos
      node.Angle <- Binding.tileTextureAngle tile
      node.Src <- src

      coroutineNode.Add (seq {
        for _ in Coroutine.milliseconds Consts.tileSlideInterval -> ()
        node.IsDrawn <- true
        yield()
      })
    else
      coroutineNode.Add (seq {
        let firstPos = node.Position
        for t in Coroutine.milliseconds Consts.tileSlideInterval do
          node.Position <- Easing.GetEasing(EasingType.InQuad, t) * (pos - firstPos) + firstPos
          yield()

        node.Position <- pos
        node.Src <- src
        yield()
      })

  let tilesPool =
    { new NodePool<int<Model.TileId>, _, _>() with
        member __.Create() = createTile()
        member __.Update(node, (cdn: int Vector2, tile: Model.Tile), isNewTile) =
          updateTile(node, (cdn, tile), isNewTile)
    }

  let tilesBackground =
    let pl = Helper.calcTilePos (Consts.boardSize)
    RectangleNode(
      Color    = Consts.backGroundColor,
      Position = Consts.tilesPos,
      Size     = pl,
      ZOrder   = ZOrder.board 0
    )

  let nextsPool =
    { new NodePool<int<Model.TileId>, _, _>() with
        member __.Create() = createTile()
        member __.Update(node, (index: int, tile: Model.Tile), isNewTile) =
          updateTile(node, ((Vector2.init (Consts.nextsCount - index - 1) 0), tile), isNewTile)
    }

  let nextsBackground =
    let pl = Helper.calcTilePos (Vector2.init (Consts.nextsCount) 1)
    RectangleNode(
      Color    = Consts.backGroundColor,
      Position = Consts.nextsPos,
      Scale = Vector2F(1.0f, 1.0f) * Consts.nextsScale,
      Size     = pl,
      ZOrder   = ZOrder.board 0
    )

  override this.OnAdded() =
    this.AddChildNode(coroutineNode)

    this.AddChildNode(tilesBackground)
    tilesBackground.AddChildNode(tilesPool)

    this.AddChildNode(nextsBackground)
    nextsBackground.AddChildNode(nextsPool)

  interface IObserver<Model.Board> with
    member __.OnCompleted() = ()
    member __.OnError(_) = ()
    member __.OnNext(board) =
      tilesPool.Update(seq {
        for (cdn, t) in Board.getTiles board -> (t.id, (cdn, t))
      })

      nextsPool.Update(seq {
        for (i, t) in Seq.indexed board.nextTiles -> (t.id, (i, t))
      })
