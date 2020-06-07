namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Board.Model
open System
open System.Threading.Tasks
open Affogato
open Altseed

type GameInfoNode(centerPosition) =
  inherit Node()

  let coroutineNode = CoroutineNode()

  let scoreFont = Font.LoadDynamicFont("mplus-1c-regular.ttf", 120)
  let timeFont = Font.LoadDynamicFont("mplus-1c-regular.ttf", 120)

  let separateLine =
    LineNode(
      Position = centerPosition,
      Thickness = 5.0f,
      Point1 = Vector2F(-Consts.gameinfoSeparateLineLength * 0.5f, 0.0f),
      Point2 = Vector2F(Consts.gameinfoSeparateLineLength * 0.5f, 0.0f),
      Color = Consts.gameInfoColor,
      ZOrder = ZOrder.GameInfo.text
    )

  let scoreText =
    TextNode(
      Font = scoreFont,
      Position = Vector2F(0.0f, -Consts.gameinfoMerginY),
      Color = Consts.gameInfoColor,
      ZOrder = ZOrder.GameInfo.text
    )

  let timeText =
    TextNode(
      Font = timeFont,
      Position = Vector2F(0.0f, Consts.gameinfoMerginY),
      Color = Consts.gameInfoColor,
      ZOrder = ZOrder.GameInfo.text
    )

  let updateScoreText text =
    if scoreText.Text <> text then
      scoreText.Text <- text
      scoreText.AdjustSize()
      scoreText.CenterPosition <- Vector2F(scoreText.Size.X * 0.5f, scoreText.Size.Y)

  let updateTimeText text =
    if timeText.Text <> text then
      timeText.Text <- text
      timeText.AdjustSize()
      timeText.CenterPosition <- Vector2F(timeText.Size.X * 0.5f, 0.0f)

  do
    updateScoreText "0"
    updateTimeText "00:00"

    coroutineNode.Add(seq {
      let mutable t = 0.0f
      while true do
        t <- t + Engine.DeltaSecond
        updateTimeText <| sprintf "%02i:%02i" (t / 60.0f |> int) (t % 60.0f |> int)
        
        yield()
    })

    base.AddChildNode(coroutineNode)
    base.AddChildNode(separateLine)
    separateLine.AddChildNode(scoreText)
    separateLine.AddChildNode(timeText)

  interface IObserver<Board> with
    member __.OnCompleted() = ()
    member __.OnError(_) = ()
    member __.OnNext(board) =
      updateScoreText <| sprintf "%d" board.point
      ()
