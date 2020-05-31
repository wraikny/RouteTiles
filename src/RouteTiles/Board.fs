namespace RouteTiles.App

open RouteTiles.Core.Model

open Affogato
open Altseed

type Board() =
  inherit Node()

  let tilePos = Vector2F(20.f, 20.0f)
  let tileSize = Vector2F(100.0f, 100.0f)
  let tileMergin = Vector2F(10.0f, 10.0f)

  let tilesPool =
    { new NodePool<int, RectangleNode, _>() with
        member __.Create() =
          RectangleNode(
            Size   = tileSize,
            Color  = Color(15, 15, 15, 255),
            ZOrder = ZOrder.board 1
          )

        member __.Update(node, (cdn: int Vector2, _dir: TileDir)) =
          node.Position <- tilePos + tileMergin + (tileSize + tileMergin) * (Vector2F(float32 cdn.x, float32 cdn.y))
    }

  let background =
    RectangleNode(
      Color    = Color(150, 150, 150, 255),
      Position = tilePos,
      Size     = Vector2F(400.0f, 600.0f - 2.0f * tilePos.Y),
      ZOrder   = ZOrder.board 0
    )

  override this.OnAdded() =
    this.AddChildNode(background)
    this.AddChildNode(tilesPool)
