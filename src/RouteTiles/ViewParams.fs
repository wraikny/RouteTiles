namespace RouteTiles.App

open System
open Altseed2
open Affogato

open RouteTiles.Core.Utils

module Consts =
  // -- Core --
  module Core =
    let [<Literal>] nextsCount = 5
    let boardSize = Vector2.init 4 5

  // -- View --
  module ViewCommon =
    let windowSize = Vector2I(1280, 720)
    let clearColor = Color(50, 50, 50, 255)

    let font = @"mplus-1c-regular.ttf"

  module PostEffect =
    let [<Literal>] wavepath = @"Shader/wave.hlsl"

  module Board =
    let [<Literal>] nextsCountToShow = 5

    // let nextsPos = Vector2F(550.0f, 50.0f)
    let nextsScale = 0.6f

    // let boardViewPos = Vector2F(100.f, 120.0f)
    let tileSize = Vector2F(100.0f, 100.0f)
    let tileMergin = Vector2F(10.0f, 10.0f)

    let backGroundColor = Color(100, 100, 100, 255)

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
    let color = Color(30, 30, 30, 255)
    let [<Literal>] merginY = 0.0f
    let [<Literal>] lineLength = 400.0f
    let [<Literal>] lineWidth = 5.0f
    let [<Literal>] fontSize = 120

  module SoloGame =
    let [<Literal>] nextsBoardMergin = 200.0f

  module Menu =
    let mainMenuRatio = 10.0f / 16.0f

    [<Literal>]
    let selectedTimePeriod = 2.0f

    let elementBackground = Nullable <| Color(200uy, 200uy, 200uy, 150uy)
    
    let iconColor = Nullable <| Color(50uy, 50uy, 50uy, 255uy)
    let cursorColor = Color(255uy, 255uy, 0uy, 0uy)
    let cursorAlphaMinMax = (0.2f, 0.6f)

    let textColor = Nullable <| Color(0uy, 0uy, 0uy)
    
    let [<Literal>] timeAttackTexture = @"menu/stopwatch.png"
    let [<Literal>] scoreAttackTexture = @"menu/hourglass.png"
    let [<Literal>] questionTexture = @"menu/question.png"
    let [<Literal>] rankingTexture = @"menu/crown.png"
    let [<Literal>] achievementTexture = @"menu/trophy.png"
    let [<Literal>] settingTexture = @"menu/gear.png"

  open System.Threading

  let initialize (progress: unit -> int) =
    let textures =
      [| Board.tileTexturePath
         Board.tileVanishmentEffectTexturePath
         Menu.timeAttackTexture
         Menu.scoreAttackTexture
         Menu.questionTexture
         Menu.rankingTexture
         Menu.achievementTexture
         Menu.settingTexture
      |]

    let gameInfoChars = "0123456789:"

    let pSum = 1 + textures.Length + gameInfoChars.Length

    pSum, async {
    let ctx = SynchronizationContext.Current
    do! Async.SwitchToThreadPool()

    let! texturesLoad =
      async {
        textures
        |> Seq.iter(fun (path) ->
          path |> Texture2D.Load |> ignore
          progress() |> ignore
        )
      } |> Async.StartChild

    let gameInfoFont = Font.LoadDynamicFont(ViewCommon.font, GameInfo.fontSize)
    progress() |> ignore

    do! Async.SwitchToContext ctx

    let Step = 10
    for c in gameInfoChars do
      gameInfoFont.GetGlyph(int c) |> ignore
      if progress () % Step = 0 then
        do! Async.Sleep 1

    do! texturesLoad
  }

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
      let x = nextsViewPos.X + nextsViewSize.X * 0.5f * nextsScale
      let y = boardViewPos.Y + boardViewSize.Y * 0.5f
      Vector2F(x, y)


module ZOrder =
  let posteffect = -100

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

  module Menu =
    let offset = (|||) (100 <<< 16)
    let background = offset 0

    let footer = offset 10
    let iconBackground = offset 20
    let icon = offset 21
    let iconSelected = offset 22

    let sideMenuBackground = offset 31
    let sideMenuText = offset 32
