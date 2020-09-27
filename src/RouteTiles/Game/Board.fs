namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.Board
open RouteTiles.Core.Board
open System
open System.Collections.Generic
open System.Threading.Tasks
open Affogato
open Altseed2

module internal BoardHelper =
  let createTile() =
    let node =
      SpriteNode(
        Texture = Texture2D.LoadStrict(Consts.Board.tileTexturePath),
        Src = RectF(Vector2F(), Consts.Board.tileSize),
        ZOrder = ZOrder.Board.tiles
      )

    node.CenterPosition <- Consts.Board.tileSize / 2.0f

    node

  let updateTile (node: SpriteNode, (cdn, tile), isNewTile) =
    let pos = Helper.Board.calcTilePosCenter cdn
    let src = Binding.Board.tileTextureSrc tile.dir tile.routeState

    if isNewTile then
      node.IsDrawn <- false
      node.Position <- pos
      node.Angle <- Binding.Board.tileTextureAngle tile.dir
      node.Src <- src

      seq {
        for _ in Coroutine.milliseconds Consts.Board.tileSlideInterval -> ()
        node.IsDrawn <- true
        yield()
      }
    else
      seq {
        let firstPos = node.Position
        for t in Coroutine.milliseconds Consts.Board.tileSlideInterval do
          node.Position <- Easing.GetEasing(EasingType.InQuad, t) * (pos - firstPos) + firstPos
          yield()

        node.Position <- pos
        node.Src <- src
        
        // Consts.Board.tilesVanishInterval
        // æ¶ˆåŽ»
        // Effectè¿½åŠ 

        yield()
      }

type internal BoardNode(addCoroutine) =
  inherit TransformNode()

  let updateTile = BoardHelper.updateTile >> addCoroutine

  // let transform = TransformNode(Position = boardPosition)
  
  let tilesBackground =
    RectangleNode(
      Color = Consts.Board.backGroundColor,
      RectangleSize = Helper.Board.boardViewSize,
      ZOrder = ZOrder.Board.background
    )

  let tilesPool = DrawnNodePool.init BoardHelper.createTile updateTile

  let cursorX =
    RectangleNode(
      Color = Consts.Board.cursorColor,
      RectangleSize = Helper.Board.cursorXSize,
      ZOrder = ZOrder.Board.cursor
    )

  let cursorY =
    RectangleNode(
      Color = Consts.Board.cursorColor,
      RectangleSize = Helper.Board.cursorYSize,
      ZOrder = ZOrder.Board.cursor
    )

  let mutable cursorMemo = ValueNone
  let mutable cursorTime = 0.0f
  let setCusorMemo c =
    cursorMemo <- ValueSome c
    cursorTime <- 0.0f

  do
    addCoroutine(seq {
      while true do
        cursorTime <- cursorTime + Engine.DeltaSecond

        let t = cursorTime / (float32 Consts.Board.cursorColorFlashingPeriod / 1000.0f)
        let col =
          Helper.lerpColor
            Consts.Board.backGroundColor
            Consts.Board.cursorColor
            (Consts.Board.cursorColorMin + (1.0f - Consts.Board.cursorColorMin) * (cos t * cos t))
        cursorX.Color <- col
        cursorY.Color <- col

        yield()
    })

  do
    let markers = [|
      for x in [-1; Consts.Core.boardSize.x] do
      for y in 1..3 do
        yield (x, y, Vector2F(float32 <| sign x, 0.0f))

      for y in [-1; Consts.Core.boardSize.y] do
      for x in 1..2 do
        yield (x, y, Vector2F(0.0f, float32 <| sign y))
    |]

    let markerSize = Vector2F(1.0f, 1.0f) * 20.0f
    for (x, y, offset) in markers do
      let node =
        RectangleNode(
          Position = -20.0f * offset + Helper.Board.calcTilePosCenter (Vector2.init x y),
          CenterPosition = markerSize * 0.5f,
          RectangleSize = markerSize,
          Color = Color(255, 255, 100, 255),
          Angle = 45.0f,
          ZOrder = ZOrder.Board.tiles
        )

      base.AddChildNode(node)

  let vanishmentEffectPool =
    { new EffectPool((Consts.Core.boardSize.x * Consts.Core.boardSize.y) / 2) with
      member x.InitEffect(node) =
        node.Count <- Vector2I(5, 9)
        node.Second <- Consts.Board.tilesVanishAnimatinTime |> Helper.toSecond
        node.IsLooping <- false

        node.Texture <- Texture2D.LoadStrict(Consts.Board.tileVanishmentEffectTexturePath)
        node.ZOrder <- ZOrder.Board.particles

        let size = (node.Texture.Size / node.Count).To2F()
        node.Src <- RectF(Vector2F(), size)
        node.CenterPosition <- size * 0.5f
        node.Scale <- Vector2F(1.0f, 1.0f) * 128.0f / size
    }

  let addVanishmentEffect(cdn, color) =
    vanishmentEffectPool.AddEffect(fun node ->
      node.Position <- Helper.Board.calcTilePosCenter(cdn)
      node.Color <- color
    )

  do
    base.AddChildNode(tilesBackground)
    base.AddChildNode(cursorX)
    base.AddChildNode(cursorY)
    base.AddChildNode(tilesPool)
    base.AddChildNode(vanishmentEffectPool)

  member __.EmitVanishmentParticle(particleSet) =
    for x in particleSet do
      x |> function
      | RouteOrLoop.Route ps ->
        for (p, _) in ps do
          addVanishmentEffect(p, Consts.Board.routeColor)
      | RouteOrLoop.Loop ps ->
        for (p, _) in ps do
          addVanishmentEffect(p, Consts.Board.loopColor)

  member __.OnNext(board) =
    tilesPool.Update(seq {
      for (cdn, t) in Model.getTiles board -> (t.id, (cdn, t))
    })

    cursorMemo |> function
    | ValueSome(c) when c = board.cursor -> ()
    | _ ->
      setCusorMemo board.cursor

      let cursorXPos, cursorYPos = Helper.Board.cursorPos board.cursor
      cursorX.Position <- cursorXPos
      cursorY.Position <- cursorYPos

  member __.Clear() =
    tilesPool.Clear()
    vanishmentEffectPool.Clear()