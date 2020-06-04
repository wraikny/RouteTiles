namespace RouteTiles.App

open RouteTiles.Core.Utils

open Altseed
open Affogato

module Consts =

  let nextsCount = 3
  let boardSize = Vector2.init 4 5

  let nextsPos = Vector2F(550.0f, 50.0f)
  let nextsScale = 0.8f

  let tilesPos = Vector2F(50.f, 120.0f)
  let tileSize = Vector2F(100.0f, 100.0f)
  let tileMergin = Vector2F(10.0f, 10.0f)

  let backGroundColor = Color(100, 100, 100, 255)

  let tileSlideInterval = 150<millisec>

module Binding =
  open RouteTiles.Core
  open RouteTiles.Core.Model
  open RouteTiles.Core.Utils

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

module Helper =
  let calcTilePos({Vector2.x=x; y=y}) = Consts.tileMergin + (Consts.tileSize + Consts.tileMergin) * (Vector2F(float32 x, float32 y))

  let calcTilePosCenter cdn = Consts.tileSize * 0.5f + calcTilePos cdn


module ZOrder =
  let board = (|||) (10 <<< 16)
