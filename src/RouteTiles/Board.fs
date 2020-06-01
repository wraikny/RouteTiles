namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Model
open RouteTiles.Core.Utils
open System
open System.Threading.Tasks
open Affogato
open Altseed

module Helper =
  let colorModeDelta colorMode =
    colorMode
    |> function
    | ColorMode.Default -> 0
    | ColorMode.Route -> 1

  let tileTextureSrc (tile: Tile) =
    let (x, y) = tile.dir |> function
      | TileDir.Empty -> (0, 2)

      | TileDir.Cross -> (3, 0)

      | TileDir.UpDown
      | TileDir.RightLeft -> (colorModeDelta tile.colorMode, 0)

      | TileDir.UpRight
      | TileDir.RightDown
      | TileDir.DownLeft
      | TileDir.UpLeft -> (colorModeDelta tile.colorMode, 1)

    RectF(Vector2F(float32 x, float32 y) * 100.0f, Vector2F(100.0f, 100.0f))


  let tileTextureAngle (tile: Tile) =
    tile.dir |> function
    | TileDir.Cross
    | TileDir.Empty
    | TileDir.UpRight
    | TileDir.UpDown-> 0.0f

    | TileDir.RightLeft
    | TileDir.RightDown -> 90.0f

    | TileDir.DownLeft -> 180.0f
    | TileDir.UpLeft -> 270.0f

type Board() =
  inherit Node()

  let calcTilePos({Vector2.x=x; y=y}) = Consts.tileMergin + (Consts.tileSize + Consts.tileMergin) * (Vector2F(float32 x, float32 y))
  
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
    let pos = Consts.tileSize * 0.5f + calcTilePos cdn
    let src = Helper.tileTextureSrc tile

    if isNewTile then
      node.IsDrawn <- false
      node.Position <- pos
      node.Angle <- Helper.tileTextureAngle tile
      node.Src <- src

      async {
        do! Async.Sleep (int Consts.tileSlideInterval)
        node.IsDrawn <- true
      }
    else
      async {
        let firstPos = node.Position
        let mutable t = 0.0f
        while t * 1000.0f < float32 Consts.tileSlideInterval do
          t <- t + Engine.DeltaSecond
          node.Position <- Easing.GetEasing(EasingType.InQuad, t * 1000.0f / (float32 Consts.tileSlideInterval)) * (pos - firstPos) + firstPos
          do! Async.Sleep(1)
        node.Position <- pos
        node.Src <- src
      }

  let tilesPool =
    { new NodePool<int<Model.TileId>, _, _>() with
        member __.Create() = createTile()
        member __.Update(node, (cdn: int Vector2, tile: Model.Tile), isNewTile) =
          updateTile(node, (cdn, tile), isNewTile)
          |> Async.StartImmediate
    }

  let tilesBackground =
    let pl = calcTilePos (Consts.boardSize)
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
          |> Async.StartImmediate
    }

  let nextsBackground =
    let pl = calcTilePos (Vector2.init (Consts.nextsCount) 1)
    RectangleNode(
      Color    = Consts.backGroundColor,
      Position = Consts.nextsPos,
      Scale = Vector2F(1.0f, 1.0f) * Consts.nextsScale,
      Size     = pl,
      ZOrder   = ZOrder.board 0
    )

  override this.OnAdded() =
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
