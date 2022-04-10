module internal RouteTiles.App.Menu.GameInfoElement

open System
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core.Types
open RouteTiles.Menu
open RouteTiles.App
open RouteTiles.App.BoxUIElements
open RouteTiles.App.Menu.ElementCommon


let createGameInfo (container: Container) name =
  let createText font fontSize text color =
    Text.Create
      ( font = font
      , fontSize = fontSize
      , text = text
      , color = color
      , zOrder = ZOrder.Menu.gameInfoText
      )
    |> BoxUI.alignY Align.Center

  let nameElem = createText container.Font 32.0f name Consts.GameInfo.nameColor
  let dataElem = createText container.Font 32.0f "0" Consts.GameInfo.dataColor

  let updater data =
    dataElem.Node.Text <- replaceOne data

  let elem =
    twoSplitFrame ZOrder.Menu.gameInfoFrame container
    |> BoxUI.withChildren [|
      empty ()
      |> BoxUI.marginLeft (LengthScale.Fixed, twoSplitFrameXMargin)
      |> BoxUI.withChild (
        nameElem
      )

      empty ()
      |> BoxUI.marginLeft (LengthScale.Fixed, gameInfoFrameSize.X * 0.5f + twoSplitFrameXMargin)
      |> BoxUI.withChild (
        dataElem
      )
    |]

  elem, updater


let createSologameInfoElement (container: Container) =
  
  let gi = container.TextMap.gameInfo
  let scoreElem, scoreUpdater = createGameInfo container gi.score
  let timeElem, timeUpdater = createGameInfo container gi.time
  let tileCountElem, tileCountUpdater = createGameInfo container gi.tileCount
  let routeCountElem, routeCountUpdater = createGameInfo container gi.routeCount
  let loopCountElem, loopCountUpdater = createGameInfo container gi.loopCount


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

  let updater (model: SoloGame.Model): unit =
    model.mode |> function
    | SoloGame.Mode.TimeAttack maxScore ->
      sprintf "%d/%d" ((maxScore - model.board.point) |> max 0) maxScore
    | _ -> sprintf "%d" model.board.point
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
    secondToDisplayTime second |> timeUpdater

  element, updater, timeUpdater
