namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Board.Model
open System
open System.Collections.Generic
open System.Threading.Tasks
open Affogato
open Altseed2

type BoardNode(boardPosition) =
  inherit Node()

  let coroutineNode = CoroutineNode()

  let createTile() =
    let node =
      SpriteNode(
        Texture = Texture2D.LoadStrict(@"tiles.png"),
        Src = RectF(Vector2F(), Consts.tileSize),
        ZOrder = ZOrder.Board.tiles
      )

    node.AdjustSize()
    node.CenterPosition <- Consts.tileSize / 2.0f

    node

  let updateTile (node: SpriteNode, (cdn, tile), isNewTile) =
    let pos = Helper.calcTilePosCenter cdn
    let src = Binding.tileTextureSrc tile.dir tile.routeState

    if isNewTile then
      node.IsDrawn <- false
      node.Position <- pos
      node.Angle <- Binding.tileTextureAngle tile.dir
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
        
        // Consts.tilesVanishInterval
        // æ¶ˆåŽ»
        // Effectè¿½åŠ 

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
      Color    = Consts.boardBackGroundColor,
      Position = boardPosition,
      Size     = Helper.boardViewSize,
      ZOrder   = ZOrder.Board.background
    )

  let cursorX =
    RectangleNode(
      Color = Consts.cursorColor,
      Size = Helper.cursorXSize,
      ZOrder = ZOrder.Board.cursor
    )

  let cursorY =
    RectangleNode(
      Color = Consts.cursorColor,
      Size = Helper.cursorYSize,
      ZOrder = ZOrder.Board.cursor
    )

  let mutable cursorMemo = ValueNone
  let mutable cursorTime = 0.0f
  let setCusorMemo c =
    cursorMemo <- ValueSome c
    cursorTime <- 0.0f

  do
    coroutineNode.Add(seq {
      while true do
        cursorTime <- cursorTime + Engine.DeltaSecond

        let t = cursorTime / (float32 Consts.cursorColorFlashingPeriod / 1000.0f)
        let col =
          Helper.lerpColor
            Consts.boardBackGroundColor
            Consts.cursorColor
            (Consts.cursorColorMin + (1.0f - Consts.cursorColorMin) * (cos t * cos t))
        cursorX.Color <- col
        cursorY.Color <- col

        yield()
    })

  let nextsPool =
    { new NodePool<int<TileId>, _, _>() with
        member __.Create() = createTile()
        member __.Update(node, (index: int, tile: Tile), isNewTile) =
          updateTile(node, (Helper.nextsIndexToCoordinate index, tile), isNewTile)
    }

  let nextsBackground =
    RectangleNode(
      Color    = Consts.boardBackGroundColor,
      Position = boardPosition + Helper.nextsViewPos,
      Scale = Vector2F(1.0f, 1.0f) * Consts.nextsScale,
      Size     = Helper.nextsViewSize,
      ZOrder   = ZOrder.Board.background
    )

  do
    let markers = [|
      for x in [-1; Consts.boardSize.x] do
      for y in 1..3 do
        yield (x, y, Vector2F(float32 <| sign x, 0.0f))

      for y in [-1; Consts.boardSize.y] do
      for x in 1..2 do
        yield (x, y, Vector2F(0.0f, float32 <| sign y))
    |]

    for (x, y, offset) in markers do
      let node =
        RectangleNode(
          Position = 20.0f * -offset + Helper.calcTilePosCenter (Vector2.init x y),
          Size = Vector2F(1.0f, 1.0f) * 20.0f,
          Color = Color(255, 255, 100, 255),
          Angle = 45.0f,
          ZOrder = ZOrder.Board.tiles
        )
      
      // node.AdjustSize()
      node.CenterPosition <- node.Size / 2.0f

      tilesBackground.AddChildNode(node)

  let vanishmentEffectPool =
    { new EffectPool((Consts.boardSize.x * Consts.boardSize.y) / 2) with
      member x.InitEffect(node) =
        node.Count <- Vector2I(5, 9)
        node.Second <- Consts.tilesVanishAnimatinTime |> Helper.toSecond
        node.IsLooping <- false

        node.Texture <- Texture2D.LoadStrict(@"tileVanishEffect.png")
        node.ZOrder <- ZOrder.Board.particles

        let size = (node.Texture.Size / node.Count).To2F()
        node.Src <- RectF(Vector2F(), size)
        node.AdjustSize()
        node.CenterPosition <- size * 0.5f
        node.Scale <- Vector2F(1.0f, 1.0f) * 128.0f / size
    }

  let addVanishmentEffect(cdn, color) =
    vanishmentEffectPool.AddEffect(fun node ->
      node.Position <- Helper.calcTilePosCenter(cdn)
      node.Color <- color
    )

  do
    base.AddChildNode(coroutineNode)

    base.AddChildNode(tilesBackground)
    tilesBackground.AddChildNode(cursorX)
    tilesBackground.AddChildNode(cursorY)
    tilesBackground.AddChildNode(tilesPool)
    tilesBackground.AddChildNode(vanishmentEffectPool)

    base.AddChildNode(nextsBackground)
    nextsBackground.AddChildNode(nextsPool)

  interface IObserver<Board> with
    member __.OnCompleted() = ()
    member __.OnError(_) = ()
    member __.OnNext(board) =
      tilesPool.Update(seq {
        for (cdn, t) in Board.getTiles board -> (t.id, (cdn, t))
      })

      nextsPool.Update(
        seq {
          let n1, n2 = board.nextTiles
          yield! n1
          yield! n2
        }
        |> Seq.indexed
        |> Seq.map(fun (i, t) -> (t.id, (i, t)))
        |> Seq.take Consts.nextsCountToShow
      )

      cursorMemo |> function
      | ValueSome(c) when c = board.cursor -> ()
      | _ ->
        setCusorMemo board.cursor

        let cursorXPos, cursorYPos = Helper.cursorPos board.cursor
        cursorX.Position <- cursorXPos
        cursorY.Position <- cursorYPos

      coroutineNode.Add(seq {
        yield! Coroutine.sleep Consts.inputInterval

        for x in board.routesAndLoops do
          x |> function
          | RouteOrLoop.Route ps ->
            for (p, _) in ps do
              addVanishmentEffect(p, Consts.routeColor)
          | RouteOrLoop.Loop ps ->
            for (p, _) in ps do
              addVanishmentEffect(p, Consts.loopColor)

        yield()
      })
