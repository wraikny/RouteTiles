namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Utils
open System
open System.Threading.Tasks
open Affogato
open Altseed

type Board(slideInterval: int<milisec>) =
  inherit Node()

  let tilesPos = Vector2F(50.f, 100.0f)
  let tileSize = Vector2F(100.0f, 100.0f)
  let tileMergin = Vector2F(10.0f, 10.0f)

  let boardSize = Vector2.init 4 5

  let backGroundColor = Color(100, 100, 100, 255)

  let calcTilePos({Vector2.x=x; y=y}) = tileMergin + (tileSize + tileMergin) * (Vector2F(float32 x, float32 y))

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
          let pos = tileSize * 0.5f + calcTilePos cdn

          if isFirstUpdate then
            async {
              node.IsDrawn <- false
              do! Async.Sleep (int slideInterval)

              let ((x, y), angle) = tile.dir |> function
                | Model.TileDir.UpDown -> ((0, 0), 0.0f)
                | Model.TileDir.RightLeft -> ((0, 0), 90.0f)
                | Model.TileDir.UpRight -> ((0, 1), 0.0f)
                | Model.TileDir.RightDown -> ((0, 1), 90.0f)
                | Model.TileDir.DownLeft -> ((0, 1), 180.0f)
                | Model.TileDir.UpLeft -> ((0, 1), 270.0f)
                | Model.TileDir.Cross -> ((3, 0), 0.0f)
                | Model.TileDir.Empty -> ((0, 2), 0.0f)

              node.Position <- pos
              node.Src <- RectF(Vector2F(float32 x, float32 y) * 100.0f, Vector2F(100.0f, 100.0f))
              node.Angle <- angle

              node.IsDrawn <- true
            } |> Async.StartImmediate
          else
            async {
              let firstPos = node.Position
              let mutable t = 0.0f
              while t * 1000.0f < float32 slideInterval do
                t <- t + Engine.DeltaSecond
                node.Position <- Easing.GetEasing(EasingType.InQuad, t * 1000.0f / (float32 slideInterval)) * (pos - firstPos) + firstPos
                do! Async.Sleep(1)
              node.Position <- pos
            } |> Async.StartImmediate
    }

  let tilesBackground =
    let pl = calcTilePos (boardSize)
    RectangleNode(
      Color    = backGroundColor,
      Position = tilesPos,
      Size     = pl,
      ZOrder   = ZOrder.board 0
    )

  // let nextsPool =
  //   ()
    
  // let nextBackground =
  //   RectangleNode(
  //     Color    = backGroundColor,
  //     // Position = tilePos,
  //     // Size     = pl,
  //     ZOrder   = ZOrder.board 0
  //   )

  override this.OnAdded() =
    this.AddChildNode(tilesBackground)
    tilesBackground.AddChildNode(tilesPool)

  interface IObserver<Model.Board> with
    member __.OnCompleted() = ()
    member __.OnError(_) = ()
    member __.OnNext(board) =
      let tiles =
        board
        |> Model.Board.getTiles
        |> Seq.map(fun (tile, cdn) -> (tile.id, (tile, cdn)))

      tilesPool.Update(tiles)
