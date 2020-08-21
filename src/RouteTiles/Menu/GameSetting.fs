module RouteTiles.App.Menu.GameSetting

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


let gameStart() =
  let elem = textButtonDesc "ゲームスタート"
  highlightenSelected true -0.02f elem

  FixedSize.Create(Vector2F(300.0f, 150.0f))
  |> BoxUI.alignX Align.Center
  |> BoxUI.withChild elem
  :> Element

module TimeAttack =
  let modeDescs = dict [|
    GameSettingMode.ModeIndex
    , { name = "目標スコア"; desc = "より短い時間で目標スコアを\n目指します。" }
    GameSettingMode.Controller
    , { name = "コントローラー"; desc = "使用するコントローラーを\n選択します。" }
    GameSettingMode.GameStart
    , { name = "ゲームスタート"; desc = "ゲームを開始します。" }
  |]

  let modeNames =
    [| for x in modeDescs -> x.Value.name |]

  let scoreNames = 
    timeAttackScores
    |> Array.map(sprintf "%d 点")

module ScoreAttack =
  let modeDescs = dict [|
    GameSettingMode.ModeIndex
    , { name = "制限時間"; desc = "時間内に多くのスコアを\n取るのが目標です。" }
    GameSettingMode.Controller
    , { name = "コントローラー"; desc = "使用するコントローラーを\n選択します。" }
    GameSettingMode.GameStart
    , { name = "ゲームスタート"; desc = "ゲームを開始します。" }
  |]

  let modeNames =
    [| for x in modeDescs -> x.Value.name |]

  let secNames =
    scoreAttackSecs
    |> Array.map(fun x ->
      sprintf "%d 分" (x / 60)
    )

let private gameSettingVerticalSelecter modeNames (setting: GameSettingState) =
  setting.mode |> function
  | GameSettingMode.ModeIndex ->
    verticalSelecter (40.0f, 10.0f) textButtonDesc modeNames setting.verticalCursor setting.index
  | GameSettingMode.Controller ->
    let currentInedx = 
      setting.controllers
      |> Array.tryFindIndex((=) setting.selectedController)
      |> Option.defaultValue -1

    verticalSelecter (40.0f, 10.0f) textButtonDesc setting.ControllerNames setting.controllerCursor currentInedx
  | GameSettingMode.GameStart ->
    gameStart()
  |> BoxUI.marginTop (LengthScale.Relative, 0.1f)

let element (gameMode, setting) =
  let modeNames, selectionNames, descs = gameMode |> function
    | SoloGameMode.TimeAttack ->
      TimeAttack.modeNames, TimeAttack.scoreNames, TimeAttack.modeDescs
    | SoloGameMode.ScoreAttack ->
      ScoreAttack.modeNames, ScoreAttack.secNames, ScoreAttack.modeDescs

  let itemMain =
    mainMenuArea [|
      split2 ColumnDir.Y 0.08f
        (settingHeader modeNames setting.mode.ToInt)
        (setting |> gameSettingVerticalSelecter selectionNames |> BoxUI.marginTop (LengthScale.Relative, 0.1f))
    |]

  split2
    ColumnDir.X
    Consts.Menu.mainMenuRatio
    itemMain
    (sideBar descs.[setting.mode])