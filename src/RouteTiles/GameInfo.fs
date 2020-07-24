namespace RouteTiles.App

open RouteTiles.Core
open RouteTiles.Core.Types.Board
open System
open System.Threading.Tasks
open Affogato
open Altseed2

type GameInfoNode(centerPosition) =
  inherit Node()

  let font = Font.LoadDynamicFontStrict("mplus-1c-regular.ttf", 120)

  let separateLine =
    LineNode(
      Position = centerPosition,
      Thickness = 5.0f,
      Point1 = Vector2F(-Consts.GameInfo.gameinfoSeparateLineLength * 0.5f, 0.0f),
      Point2 = Vector2F(Consts.GameInfo.gameinfoSeparateLineLength * 0.5f, 0.0f),
      Color = Consts.GameInfo.gameInfoColor,
      ZOrder = ZOrder.GameInfo.text
    )

  let scoreText =
    TextNode(
      Font = font,
      Position = Vector2F(0.0f, -Consts.GameInfo.gameinfoMerginY),
      Color = Consts.GameInfo.gameInfoColor,
      ZOrder = ZOrder.GameInfo.text
    )

  let timeText =
    TextNode(
      Font = font,
      Position = Vector2F(0.0f, Consts.GameInfo.gameinfoMerginY),
      Color = Consts.GameInfo.gameInfoColor,
      ZOrder = ZOrder.GameInfo.text
    )

  let setScoreText text =
    if scoreText.Text <> text then
      scoreText.Text <- text
      scoreText.AdjustSize()
      scoreText.CenterPosition <- Vector2F(scoreText.Size.X * 0.5f, scoreText.Size.Y)

  let setTimeText text =
    if timeText.Text <> text then
      timeText.Text <- text
      timeText.AdjustSize()
      timeText.CenterPosition <- Vector2F(timeText.Size.X * 0.5f, 0.0f)

  do
    setScoreText "0"
    setTimeText "00:00:00"

    base.AddChildNode(separateLine)
    separateLine.AddChildNode(scoreText)
    separateLine.AddChildNode(timeText)

  member __.OnNext(board) =
    setScoreText <| sprintf "%d" board.point

  member __.SetTime(time) =
    setTimeText <| sprintf "%02i:%02i:%02i" (time / 60.0f |> int) (time % 60.0f |> int) ((time % 1.0f) * 100.0f |> int)
