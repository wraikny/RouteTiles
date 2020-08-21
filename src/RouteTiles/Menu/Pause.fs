module RouteTiles.App.Menu.Pause

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

let pauseSelectNames =
  pauseSelects
  |> Array.map(function
    | PauseSelect.Continue -> "続ける"
    | PauseSelect.Restart -> "さいしょから"
    | PauseSelect.QuitGame -> "メニューに戻る"
  )

let element (index: int) =
  Empty.Create()
  |> BoxUI.withChildren [|
    Rectangle.Create(color = Consts.Menu.pauseBackgroundColor, zOrder = ZOrder.Menu.background) :> Element
    ( verticalSelecter (60.0f, 15.0f) (textButton (fontName())) pauseSelectNames index -1
      |> BoxUI.margin (LengthScale.Relative, 0.3f)
      |> BoxUI.alignCenter
    )
  |]