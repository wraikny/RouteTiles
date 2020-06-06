namespace RouteTiles.App

open RouteTiles.Core.Board.Model
open System
open System.Threading.Tasks
open Affogato
open Altseed

type BoardNode(boardPosition) =
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
    { new NodePool<int<TileId>, _, _>() with
        member __.Create() = createTile()
        member __.Update(node, (cdn: int Vector2, tile: Tile), isNewTile) =
          updateTile(node, (cdn, tile), isNewTile)
    }

  let tilesBackground =
    RectangleNode(
      Color    = Consts.backGroundColor,
      Position = boardPosition,
      Size     = Helper.boardViewSize,
      ZOrder   = ZOrder.board 0
    )

  let nextsPool =
    { new NodePool<int<TileId>, _, _>() with
        member __.Create() = createTile()
        member __.Update(node, (index: int, tile: Tile), isNewTile) =
          updateTile(node, (Helper.nextsIndexToCoordinate index, tile), isNewTile)
    }

  let nextsBackground =
    RectangleNode(
      Color    = Consts.backGroundColor,
      Position = boardPosition + Helper.nextsViewPos,
      Scale = Vector2F(1.0f, 1.0f) * Consts.nextsScale,
      Size     = Helper.nextsViewSize,
      ZOrder   = ZOrder.board 0
    )

  override this.OnAdded() =
    this.AddChildNode(coroutineNode)

    this.AddChildNode(tilesBackground)
    tilesBackground.AddChildNode(tilesPool)

    this.AddChildNode(nextsBackground)
    nextsBackground.AddChildNode(nextsPool)

  interface IObserver<Board> with
    member __.OnCompleted() = ()
    member __.OnError(_) = ()
    member __.OnNext(board) =
      tilesPool.Update(seq {
        for (cdn, t) in Board.getTiles board -> (t.id, (cdn, t))
      })

      nextsPool.Update(seq {
        for (i, t) in Seq.indexed board.nextTiles -> (t.id, (i, t))
      })
