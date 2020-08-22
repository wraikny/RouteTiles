module RouteTiles.App.Menu.Setting

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

let private settingModeNames =
  settingModes |> Array.map(function
    | SettingMode.InputtingName
    | SettingMode.InputName -> "ユーザ名"
    | SettingMode.Background -> "背景"
    | SettingMode.Enter -> "確定"
  )

let private backgroundNames =
  backgrounds |> Array.map(function
    | Background.Wave -> "波"
  )

let private inputName text =
  FixedHeight.Create(40.0f)
  |> BoxUI.withChild(
    textButton (fontDesc()) text
  )

let settingDescs = Map.ofSeq [|
  SettingMode.InputtingName
  , { name = "入力中..."
      desc = "決定ボタンで確定します。\nEscキー/メニューボタンで\nキャンセルします。" }

  SettingMode.InputName
  , { name = "ユーザ名設定"
      desc = "デフォルトのユーザ名を\nキーボードで設定します。\n空欄の場合はプレイ毎に\n入力可能です。\n決定ボタンで入力状態に\n移行します。" }
  SettingMode.Background
  , { name = "背景設定"; desc = "好みの背景を設定可能です。" }
  SettingMode.Enter
  , { name = "確定"; desc = "設定を確定して反映します。" }
|]

let element(state: SettingState) =
  let item = state.mode |> function
    | SettingMode.InputtingName ->
      let elem = inputName (new String(state.name)) :> Element
      elem |> highlightenSelected true 0.0f
      elem
    | SettingMode.InputName ->
      (inputName <| new String(state.name)) :> Element
    | SettingMode.Background ->
      (
        verticalSelecter
          (40.0f, 10.0f)
          textButtonDesc
          backgroundNames
          state.vertCursor
          state.background
      )
    | SettingMode.Enter ->
      let elem = centeredButton (Vector2F(300.0f, 100.0f)) "確定"
      let e = Empty.Create()
      elem.AddChild(e)
      e |> highlightenSelected true -0.05f
      elem

  let itemMain =
    mainMenuArea [|
      split2 ColumnDir.Y Consts.Menu.modeHeaderRatio
        (settingHeader settingModeNames state.modeCursor)
        (item |> BoxUI.marginTop (LengthScale.Relative, 0.1f))
    |]

  split2 ColumnDir.X Consts.Menu.mainMenuRatio
    itemMain
    (sideBar <| settingDescs.[state.mode])
