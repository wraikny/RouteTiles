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

type private DataKind = Current | SelfOld | Other

let private rankingElement (dataKind: DataKind) rank name result slideCount date =
  let color = dataKind |> function
    | Current -> Nullable(Consts.Menu.cursorColor)
    | SelfOld -> Nullable(Consts.Menu.currentColor)
    | Other -> Consts.Menu.resultBackgroundColor

  Rectangle.Create(color = color, zOrder = ZOrder.Menu.iconBackground)
  |> BoxUI.withChild(
    split2 ColumnDir.X 0.1f
      (rank |> textDesc |> BoxUI.alignCenter)
      (
        Column.Create(ColumnDir.X)
        |> BoxUI.withChildren [|
          textDesc name |> BoxUI.alignCenter
          textDesc result |> BoxUI.alignCenter
          textDesc slideCount |> BoxUI.alignCenter
          textDesc date |> BoxUI.alignCenter
        |]
      )
  )

let ranking(config: Config, mode, res: GameResult, id: int64 voption, data: SimpleRankingsServer.Data<GameResult>[]) =
  let isTime = mode |> function
    | SoloGame.Mode.TimeAttack _ -> true
    | SoloGame.Mode.ScoreAttack _ -> false

  ItemList.Create(itemHeight = 40.0f, itemMargin = 10.0f)
  |> BoxUI.withChildren [|
    yield rankingElement Other "順位" "名前" (if isTime then "時間" else "スコア") "手数" "日付"
    let mutable index = 1
    for r in data do
      let kind =
        if id.IsSome && id.Value = r.id then Current
        else if config.guid = r.userId && res.Name = r.values.Name then SelfOld
        else Other

      yield
        rankingElement
          kind
          (sprintf "%d" index)
          r.values.Name
          (if isTime then sprintf "%.2f" r.values.Time else sprintf "%d" r.values.Point)
          (sprintf "%d" r.values.SlideCount)
          (r.utcDate.ToString("yyyy/MM/dd"))
      index <- index + 1
  |]
  :> Element

let element(config: Config, mode, res: GameResult, state: GameRankingState) =
  let modeName, resultText = mode |> function
    | SoloGame.Mode.TimeAttack _ ->
      "タイムアタック"
      , sprintf "時間: %.2f" res.Time

    | SoloGame.Mode.ScoreAttack _ ->
      "スコアアタック"
      , sprintf "得点: %d" res.Point

  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.background)
  |> BoxUI.withChild(
    ItemList.Create(itemMargin = 10.0f)
    |> BoxUI.marginX (LengthScale.Relative, 0.05f)
    |> BoxUI.marginY (LengthScale.Relative, 0.1f)
    |> BoxUI.withChildren [|

      match state with
      | GameRankingState.InputName name ->
        yield textItem (fontName()) (sprintf "リザルト: %s" modeName)
        yield line()
        yield textDesc resultText
        yield textDesc "キーボードで名前を入力してください。"
        yield textDesc "Enterキーを押すとスコアがランキングサーバーに投稿されます。"
        yield textDesc "Escキーを押すと投稿せずにメニューに戻ります。"
        yield textDesc "メインメニューの設定からデフォルト名を設定可能です。"
        yield line()
        yield (
          FixedHeight.Create(40.0f)
          |> BoxUI.withChild(textButtonDesc (new String(name)))
          :> Element
        )
        yield line()
      | GameRankingState.Waiting ->
        yield textDesc "ランキング情報を取得中です..."
      | GameRankingState.Error e ->
        yield textDesc "エラーが発生しました。"
        yield!
          e.ToCharArray()
          |> Array.chunkBySize 40
          |> Array.map(fun cs -> new String(cs) |> textDesc)
      | GameRankingState.Success(id, data) ->
        yield ranking(config, mode, res, ValueSome id, data)
    |]
  )
  :> Element
