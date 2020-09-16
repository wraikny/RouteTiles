module RouteTiles.App.MenuV2.GameInfoElement

open System
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core
open RouteTiles.App
open RouteTiles.App.BoxUIElements
open RouteTiles.App.MenuV2.MenuElement


let createGameInfo (container: Container) name =
  let createText text color =
    Text.Create
      ( font = container.Font
      , text = text
      , color = color
      , zOrder = ZOrder.Menu.gameInfoText
      )
    |> BoxUI.alignY Align.Center

  let nameElem = createText name Consts.GameInfo.nameColor
  let dataElem = createText "" Consts.GameInfo.dataColor

  let updater data =
    dataElem.Node.Text <- data

  let elem =
    Sprite.Create
      ( aspect = Aspect.Fixed
      , texture = container.GameInfoFrame
      , zOrder = ZOrder.Menu.gameInfoFrame
      )
    |> BoxUI.withChildren [|
      empty ()
      |> BoxUI.marginLeft (LengthScale.Fixed, 32.0f)
      |> BoxUI.withChild (
        nameElem
      )

      empty ()
      |> BoxUI.marginLeft (LengthScale.Fixed, float32 container.GameInfoFrame.Size.X * 0.5f + 32.0f)
      |> BoxUI.withChild (
        dataElem
      )
    |]

  elem, updater


let createSologameInfoElement (container: Container) =
  
  let scoreElem, scoreUpdater = createGameInfo container "スコア"
  let timeElem, timeUpdater = createGameInfo container "タイム"
  let tileCountElem, tileCountUpdater = createGameInfo container "タイル数"
  let routeCountElem, routeCountUpdater = createGameInfo container "ルート数"
  let loopCountElem, loopCountUpdater = createGameInfo container "ループ数"


  let element: ElementRoot =
    let boardPos = Helper.SoloGame.boardViewPos

    createBase ()
    |> BoxUI.withChild (
      empty ()
      |> BoxUI.marginTop (LengthScale.Fixed, boardPos.Y)
      |> BoxUI.marginRight (LengthScale.Fixed, 40.0f)
      |> BoxUI.withChild (
        ItemList.Create(itemMargin = 16.0f)
        // |> BoxUI.debug
        |> BoxUI.alignX Align.Max
        |> BoxUI.withChildren [|
          scoreElem
          timeElem
          tileCountElem
          routeCountElem
          loopCountElem
        |]
      )
    )

  let updater (model: Types.SoloGame.Model): unit =
    model.board.point
    |> sprintf "%d"
    |> scoreUpdater

    model.board.vanishedTilesCount
    |> sprintf "%d"
    |> tileCountUpdater

    model.board.routesHistory
    |> List.length
    |> sprintf "%d"
    |> routeCountUpdater

    model.board.loopsHistory
    |> List.length
    |> sprintf "%d"
    |> loopCountUpdater

  let timeUpdater second =
    sprintf "%02i:%02i.%03i"
      (second / 60.0f |> int)
      (second % 60.0f |> int)
      ((second % 1.0f) * 1000.0f |> int)
    |> timeUpdater

  element, updater, timeUpdater
