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

  let transform =
    RectangleNode(
      Position = centerPosition
    ) :> TransformNode

  let separateLine =
    let size = Vector2F(Consts.GameInfo.lineLength, Consts.GameInfo.lineWidth)
    RectangleNode(
      RectangleSize = size,
      CenterPosition = size * 0.5f,
      Color = Consts.GameInfo.color,
      ZOrder = ZOrder.GameInfo.text
    )

  let scoreText =
    TextNode(
      Font = font,
      Position = Vector2F(0.0f, -Consts.GameInfo.merginY),
      Color = Consts.GameInfo.color,
      ZOrder = ZOrder.GameInfo.text
    )

  let timeText =
    TextNode(
      Font = font,
      Position = Vector2F(0.0f, Consts.GameInfo.merginY),
      Color = Consts.GameInfo.color,
      ZOrder = ZOrder.GameInfo.text
    )

  let setScoreText text =
    if scoreText.Text <> text then
      scoreText.Text <- text
      let size = scoreText.ContentSize
      scoreText.CenterPosition <- Vector2F(size.X * 0.5f, size.Y)

  let setTimeText text =
    if timeText.Text <> text then
      timeText.Text <- text
      let size = timeText.ContentSize
      timeText.CenterPosition <- Vector2F(size.X * 0.5f, 0.0f)

  do
    setScoreText "0"
    setTimeText "00:00:00"

    base.AddChildNode(transform)
    transform.AddChildNode(separateLine)
    transform.AddChildNode(scoreText)
    transform.AddChildNode(timeText)

  member __.OnNext(board) =
    setScoreText <| sprintf "%d" board.point

  member __.SetTime(time) =
    setTimeText <| sprintf "%02i:%02i:%02i" (time / 60.0f |> int) (time % 60.0f |> int) ((time % 1.0f) * 100.0f |> int)
