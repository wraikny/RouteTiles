module RouteTiles.App.Menu.GameResult

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core.Types
open RouteTiles.Core.Types.Menu
open RouteTiles.App.BoxUIElements
open RouteTiles.App.Menu.Common
open RouteTiles.App

let private textItem font text =
  Text.Create(text = text, font = font, zOrder = ZOrder.Menu.buttonText, color = Consts.Menu.textColor) :> Element

let private textDesc = textItem (fontDesc())

let private rankingElement isSelf rank name point time =
  let color = if isSelf then Nullable(Consts.Menu.cursorColor) else Consts.Menu.resultBackgroundColor

  Rectangle.Create(color = color, zOrder = ZOrder.Menu.iconBackground)
  |> BoxUI.withChild(
    split2 ColumnDir.X 0.1f
      (rank |> textDesc |> BoxUI.alignCenter)
      (
        Column.Create(ColumnDir.X)
        |> BoxUI.withChildren [|
          textDesc name |> BoxUI.alignCenter
          textDesc point |> BoxUI.alignCenter
          textDesc time |> BoxUI.alignCenter
        |]
      )
  )

let element(mode, res: GameResult, state) =
  let modeName, pointText, timeText = mode |> function
    | SoloGame.Mode.TimeAttack score ->
      "タイムアタック"
      , sprintf "得点: %d/%d" res.Point score
      , sprintf "時間: %.2f" res.Time

    | SoloGame.Mode.ScoreAttack time ->
      "スコアアタック"
      , sprintf "得点: %d" res.Point
      , sprintf "時間: %.2f秒/%.2f秒" res.Time time

  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.background)
  |> BoxUI.withChild(
    ItemList.Create(itemMargin = 10.0f)
    |> BoxUI.marginX (LengthScale.Relative, 0.2f)
    |> BoxUI.marginY (LengthScale.Relative, 0.1f)
    |> BoxUI.withChildren [|
      yield textItem (fontName()) (sprintf "リザルト: %s" modeName)
      yield FixedHeight.Create(10.0f).With(Rectangle.Create()) :> Element
      yield textDesc pointText
      yield textDesc timeText

      match state with
      | GameRankingState.InputName name ->
        yield textDesc "キーボードで名前を入力してください。"
        yield textDesc "Enterキーを押すとスコアがランキングサーバーに投稿されます。"
        yield textDesc "Escキーを押すと投稿せずにメニューに戻ります。"
        yield textDesc (new String(name))
      | GameRankingState.Waiting ->
        yield textDesc "ランキング情報を取得中です..."
      | GameRankingState.Error e ->
        yield textDesc "エラーが発生しました。"
        yield textDesc e
      | GameRankingState.Success(id, res) ->
        yield (
          ItemList.Create(itemMargin = 10.0f)
          |> BoxUI.withChildren [|
            yield rankingElement false "順位" "名前" "スコア" "時間"
            let mutable index = 1
            for r in res do
              yield
                rankingElement
                  (id = r.id)
                  (sprintf "%d" index)
                  r.values.Name
                  (sprintf "%d" r.values.Point)
                  (sprintf "%.2f" r.values.Time)
              index <- index + 1
          |]
          :> Element
        )
    |]
  )
  :> Element
