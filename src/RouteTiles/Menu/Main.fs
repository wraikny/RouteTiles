module RouteTiles.App.Menu.Main

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core.Types
open RouteTiles.Core.Types.Menu
open RouteTiles.App
open RouteTiles.App.BoxUIElements
open RouteTiles.App.Menu.Common

let private modeButtons = [|
  Consts.Menu.timeAttackTexture, Mode.TimeAttack
  Consts.Menu.scoreAttackTexture, Mode.ScoreAttack
  Consts.Menu.questionTexture, Mode.VS
  Consts.Menu.rankingTexture, Mode.Ranking
  Consts.Menu.achievementTexture, Mode.Achievement
  Consts.Menu.settingTexture, Mode.Setting
|]

let private buttonIcon path = 
  let texture = Texture2D.LoadStrict path
  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.iconBackground)
  |> BoxUI.withChild(
    Sprite.Create(
      aspect = Aspect.Keep,
      zOrder = ZOrder.Menu.icon,
      texture = texture,
      color = Consts.Menu.iconColor
    )
    |> BoxUI.margin (LengthScale.RelativeMin, 0.1f)
    :> Element
  )


let modeTexts = dict <| seq {
  ( Mode.TimeAttack
    , {
      name = "タイムアタック"
      desc = "目標スコアまでの時間を\n競うモードです。"
    })
  ( Mode.ScoreAttack
    , {
      name = "スコアアタック"
      desc = "制限時間内のスコアを\n競うモードです。"
    })
  ( Mode.VS
    , {
      name = "？？？"
      desc = "未実装の機能です。"
    })
  ( Mode.Ranking
    , {
      name = "ランキング"
      desc = "オンラインランキングを\nチェックしよう！"
    })
  ( Mode.Achievement
    , {
      name = "実績"
      desc = "開放した実績を確認します。"
    })
  ( Mode.Setting
    , {
      name = "設定"
      desc = "各種設定を行います。"
    })
}


let private mainMenu (model: Model) =
  mainMenuArea [|
    Grid.Create(Vector2F(1.0f, 1.0f) * 180.0f) :> Element
    |> BoxUI.withChildren [|
      for (path, mode) in modeButtons ->
        let buttonIcon = buttonIcon path
        if mode = model.cursor then
          buttonIcon
          |> highlightenSelected true -0.05f
        buttonIcon
    |]
  |]

let element (model: Model) =
  split2
    ColumnDir.X
    Consts.Menu.mainMenuRatio
    (mainMenu model)
    (sideBar <| modeTexts.[model.cursor])
