module internal RouteTiles.App.MenuV2.GameInfoElement

open System
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core
open RouteTiles.App
open RouteTiles.App.BoxUIElements
open RouteTiles.App.MenuV2.ElementCommon


let createGameInfo (container: Container) name =
  let createText font text color =
    Text.Create
      ( font = font
      , text = text
      , color = color
      , zOrder = ZOrder.Menu.gameInfoText
      )
    |> BoxUI.alignY Align.Center

  let nameElem = createText container.Font name Consts.GameInfo.nameColor
  let dataElem = createText container.Font "" Consts.GameInfo.dataColor

  let updater data =
    dataElem.Node.Text <- data

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

  let updater (model: Types.SoloGame.Model): unit =
    model.mode |> function
    | Types.SoloGame.Mode.TimeAttack maxScore ->
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
