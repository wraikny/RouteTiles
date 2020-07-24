namespace RouteTiles.App

open RouteTiles.Core.Utils

open Altseed2
open Affogato

module Consts =
  // -- Core --
  module Core =
    let [<Literal>] nextsCount = 5
    let boardSize = Vector2.init 4 5

  // -- View --
  module ViewCommon =
    let windowSize = Vector2I(1280, 720)
    let clearColor = Color(200, 200, 200, 255)

  module Board =
    let [<Literal>] nextsCountToShow = 5

    // let nextsPos = Vector2F(550.0f, 50.0f)
    let nextsScale = 0.6f

    // let boardViewPos = Vector2F(100.f, 120.0f)
    let tileSize = Vector2F(100.0f, 100.0f)
    let tileMergin = Vector2F(10.0f, 10.0f)

    let boardBackGroundColor = Color(100, 100, 100, 255)

    let routeColor = Color(255, 255, 100, 255)
    let loopColor = Color(100, 100, 255, 255)

    let cursorColor = Color(105, 255, 220, 255)
    let [<Literal>] cursorColorMin = 0.25f

    let [<Literal>] tileTexturePath = @"tiles.png"

    let [<Literal>] tileVanishmentEffectTexturePath = @"tileVanishEffect.png"

    let [<Literal>] cursorColorFlashingPeriod = 600<millisec>
    let [<Literal>] tileSlideInterval = 120<millisec>
    let [<Literal>] tilesVanishInterval = 120<millisec>
    let [<Literal>] tilesVanishAnimatinTime = 750<millisec>

  module GameCommon =
    let pauseBackground = Color(0, 0, 0, 100)
    let [<Literal>] inputInterval = 120<millisec>
    let [<Literal>] waitingInputIntervalOnOpeningPause = 20<millisec>

  module GameInfo =
    let gameInfoColor = Color(30, 30, 30, 255)
    let [<Literal>] gameinfoMerginY = 0.0f
    let [<Literal>] gameinfoSeparateLineLength = 400.0f

  module SoloGame =
    let [<Literal>] nextsBoardMergin = 200.0f

module Binding =
  module Board =
    open Consts.Board
    open RouteTiles.Core.Types.Board

    let routeStateDelta routeState =
      routeState
      |> function
      | LineState.Default -> 0
      | LineState.Routing
      | LineState.Routed -> 1
      | LineState.Looping
      | LineState.Looped -> 2

    let tileTextureSrc dir routeState =
      let (x, y) =
        match dir, routeState with
        | TileDir.Empty, RouteState.Empty -> (0, 2)

        | TileDir.Cross, RouteState.Cross(h, v) -> (3 + routeStateDelta v, 0 + routeStateDelta h)

        | TileDir.UpDown, RouteState.Single(ss)
        | TileDir.RightLeft, RouteState.Single(ss) -> (routeStateDelta ss, 0)

        | TileDir.UpRight, RouteState.Single(ss)
        | TileDir.RightDown, RouteState.Single(ss)
        | TileDir.DownLeft, RouteState.Single(ss)
        | TileDir.UpLeft, RouteState.Single(ss) -> (routeStateDelta ss, 1)

        | _ -> failwith "Unexpected TileDir and RouteState pair"

      RectF(Vector2F(float32 x, float32 y) * tileSize, tileSize)

    let tileTextureAngle (dir) =
      dir |> function
      | TileDir.Cross
      | TileDir.Empty
      | TileDir.UpRight
      | TileDir.UpDown-> 0.0f

      | TileDir.RightLeft
      | TileDir.RightDown -> 90.0f

      | TileDir.DownLeft -> 180.0f
      | TileDir.UpLeft -> 270.0f


module Helper =
  let inline toSecond (x: int<millisec>) = float32 x / 1000.0f

  let lerpColor (x: Color) (y: Color) (t: float32) =
    let inline f a b =
      float32 a * (1.0f - t) + float32 b * t
      |> clamp 0.0f 255.0f
      |> byte
    Color(f x.R y.R, f x.G y.G, f x.B y.B, f x.A y.A)

  module Board =
    open Consts.Core
    open Consts.ViewCommon
    open Consts.Board

    let calcTilePos({Vector2.x=x; y=y}) = tileMergin + (tileSize + tileMergin) * (Vector2F(float32 x, float32 y))

    let calcTilePosCenter cdn = tileSize * 0.5f + calcTilePos cdn

    let boardViewSize = calcTilePos boardSize

    let private flipNexts: int Vector2 -> _ = Vector.yx

    let nextsIndexToCoordinate index =
      Vector2.init 0 index
      |> flipNexts

    let nextsViewSize =
      Vector2.init 1 nextsCount
      |> flipNexts
      |> calcTilePos

    let cursorXSize = calcTilePos { boardSize with x = 1 }
    let cursorYSize = calcTilePos { boardSize with y = 1 }

    let cursorPos (cdn: int Vector2) =
      calcTilePos { cdn with y = 0 } - tileMergin,
      calcTilePos { cdn with x = 0 } - tileMergin

  module SoloGame =
    open Board
    open Consts.ViewCommon
    open Consts.Board
    open Consts.SoloGame

    let boardViewPos =
      let p = (windowSize.To2F() - boardViewSize) * 0.5f
      Vector2F(150.0f, p.Y)

    let nextsViewPos =
      boardViewPos
      + Vector2F(nextsBoardMergin + boardViewSize.X, 0.0f)
    
    let gameInfoCenterPos =
      let x = boardViewPos.X + nextsViewPos.X + nextsViewSize.X * 0.5f * nextsScale
      let y = boardViewPos.Y + boardViewSize.Y * 0.5f
      Vector2F(x, y)


module ZOrder =

  module Board =
    let offset = (|||) (10 <<< 16)

    let background = offset 0
    let cursor = offset 1
    let tiles = offset 2
    let particles = offset 3

  module GameInfo =
    let offset = (|||) (20 <<< 16)
    let text = offset 0

  module Pause =
    let offset = (|||) (50 <<< 16)
    let background = offset 0
    let elementBackground = offset 1
    let text = offset 100
