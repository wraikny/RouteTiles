namespace RouteTiles.App

open RouteTiles.Core.Utils

open Altseed
open Affogato

module Consts =
  // -- Core --

  let nextsCount = 5
  let boardSize = Vector2.init 4 5

  // -- View --

  let windowSize = Vector2I(1280, 720)

  let nextsCountToShow = 5

  // let nextsPos = Vector2F(550.0f, 50.0f)
  let nextsScale = 0.6f

  // let boardViewPos = Vector2F(100.f, 120.0f)
  let tileSize = Vector2F(100.0f, 100.0f)
  let tileMergin = Vector2F(10.0f, 10.0f)
  let nextsBoardMergin = 100.0f

  let clearColor = Color(200, 200, 200, 255)
  let boardBackGroundColor = Color(100, 100, 100, 255)
  let cursorColor = Color(200, 220, 200, 255)

  let cursorColorFlashingPeriod = 750<millisec>
  let tileSlideInterval = 120<millisec>
  let inputInterval = 80<millisec>

module Binding =
  open RouteTiles.Core.Board.Model

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
  let lerpColor (x: Color) (y: Color) (t: float32) =
    let inline f a b =
      float32 a * (1.0f - t) + float32 b * t
      |> clamp 0.0f 255.0f
      |> byte
    Color(f x.R y.R, f x.G y.G, f x.B y.B, f x.A y.A)

  let calcTilePos({Vector2.x=x; y=y}) = Consts.tileMergin + (Consts.tileSize + Consts.tileMergin) * (Vector2F(float32 x, float32 y))

  let calcTilePosCenter cdn = Consts.tileSize * 0.5f + calcTilePos cdn

  let boardViewSize = calcTilePos Consts.boardSize

  let private flipNexts: int Vector2 -> _ = Vector.yx

  let nextsIndexToCoordinate index =
    Vector2.init 0 index
    |> flipNexts

  let nextsViewSize =
    Vector2.init 1 Consts.nextsCount
    |> flipNexts
    |> calcTilePos

  let nextsViewPos = Vector2F(Consts.nextsBoardMergin + boardViewSize.X, 0.0f)

  let boardViewPos =
    let p = (Consts.windowSize.To2F() - boardViewSize) * 0.5f
    Vector2F(150.0f, p.Y)

  let cursorXSize = calcTilePos { Consts.boardSize with x = 1 }
  let cursorYSize = calcTilePos { Consts.boardSize with y = 1 }

  let cursorPos (cdn: int Vector2) =
    calcTilePos { cdn with y = 0 } - Consts.tileMergin,
    calcTilePos { cdn with x = 0 } - Consts.tileMergin


module ZOrder =

  module Board =
    let offset = (|||) (10 <<< 16)

    let background = offset 0
    let cursor = offset 1
    let tiles = offset 2
