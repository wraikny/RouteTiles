namespace RouteTiles.App

open RouteTiles.Core

open System
open Affogato
open Altseed

type Board() =
  inherit Node()

  let tilePos = Vector2F(20.f, 20.0f)
  let tileSize = Vector2F(100.0f, 100.0f)
  let tileMergin = Vector2F(10.0f, 10.0f)

  let boardSize = Vector2F(4.0f, 5.0f)

  let calcPos(x, y) = 
    tileSize * 0.5f + tilePos + tileMergin + (tileSize + tileMergin) * (Vector2F(x, y))

  let tilesPool =
    let texture = Texture2D.Load(@"tiles.png")

    { new NodePool<int<Model.TileId>, _, _>() with
        member __.Create() =
          let node =
            SpriteNode(
              Texture = texture,
              Src = RectF(0.0f, 0.0f, 100.0f, 100.0f),
              ZOrder = ZOrder.board 1
            )

          node.AdjustSize()
          node.CenterPosition <- tileSize / 2.0f

          node

        member __.Update(node, (tile: Model.Tile, cdn: int Vector2), isFirstUpdate) =
          node.Position <- calcPos(float32 cdn.x, float32 cdn.y)

          if isFirstUpdate then
            let ((x, y), angle) = tile.dir |> function
              | Model.TileDir.UpDown -> ((0, 0), 0.0f)
              | Model.TileDir.RightLeft -> ((0, 0), 90.0f)
              | Model.TileDir.UpRight -> ((0, 1), 0.0f)
              | Model.TileDir.RightDown -> ((0, 1), 90.0f)
              | Model.TileDir.DownLeft -> ((0, 1), 180.0f)
              | Model.TileDir.UpLeft -> ((0, 1), 270.0f)
              | Model.TileDir.Cross -> ((3, 0), 0.0f)
              | Model.TileDir.Empty -> ((0, 2), 0.0f)

            node.Src <- RectF(Vector2F(float32 x, float32 y) * 100.0f, Vector2F(100.0f, 100.0f))
            node.Angle <- angle
    }

  let background =
    let p0 = calcPos(0.0f, 0.0f)
    let pl = calcPos(float32 boardSize.X, float32 boardSize.Y)
    RectangleNode(
      Color    = Color(100, 100, 100, 255),
      Position = tilePos,
      Size     = pl - p0 + tileMergin,
      ZOrder   = ZOrder.board 0
    )

  override this.OnAdded() =
    this.AddChildNode(background)
    this.AddChildNode(tilesPool)

  interface IObserver<Model.Board> with
    member __.OnCompleted() = ()
    member __.OnError(_) = ()
    member __.OnNext(board) =
      let tiles =
        board
        |> Model.Board.getTiles
        |> fun x -> printfn "%A" (Array.filter (fun (_, x) -> Vector.x x = 0) x); x
        |> Seq.map(fun (tile, cdn) -> (tile.id, (tile, cdn)))

      tilesPool.Update(tiles)
