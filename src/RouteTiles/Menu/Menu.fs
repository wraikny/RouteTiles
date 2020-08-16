namespace RouteTiles.App

open System
open Altseed2
open Altseed2.BoxUI

module MenuParams =
  let mainMenuRatio = 10.0f / 16.0f

  [<Literal>]
  let selectedTimePeriod = 2.0f

  module Color =
    let debugColor = Nullable <| Color(255uy, 0uy, 0uy, 100uy)
    let backgroundColor = Nullable <| Color(50uy, 50uy, 50uy)
    let sideBarColor = Nullable <| Color(150uy, 150uy, 150uy, 150uy)

    let iconBack = Nullable <| Color(240uy, 240uy, 240uy)
    let iconColor = Nullable <| Color(50uy, 50uy, 50uy, 255uy)
    let iconSelected = Color(255uy, 255uy, 0uy, 0uy)

  module Texture =
    let timeAttack = @"menu/stopwatch.png"
    let scoreAttack = @"menu/hourglass.png"
    let question = @"menu/question.png"
    let ranking = @"menu/crown.png"
    let achievement = @"menu/trophy.png"
    let setting = @"menu/gear.png"

  module ZOrder =
    let offset = (|||) (100 <<< 16)
    let background = offset 0

    let footer = offset 10
    let iconBackground = offset 20
    let icon = offset 21
    let iconSelected = offset 22

    let sideMenuBackground = offset 31
    let sideButtonText = offset 32

module MenuElement =
  open Altseed2.BoxUI.Elements
  open Altseed2.BoxUI
  open RouteTiles.App.BoxUIElements
  open MenuParams
  open MenuCore

  let private modeButtons = [|
    Texture.timeAttack, Mode.TimeAttack
    Texture.scoreAttack, Mode.ScoreAttack
    Texture.question, Mode.VS
    Texture.ranking, Mode.Ranking
    Texture.achievement, Mode.Achievement
    Texture.setting, Mode.Setting
  |]

  let mutable selectedTime = 0.0f

  let selectedSpriteOnUpdate =
    Action<RectangleNode>(
      fun (node: RectangleNode) ->
        let alpha = (1.0f + MathF.Sin(selectedTime *  2.0f * MathF.PI / selectedTimePeriod)) * 0.5f
        let alpha = (alpha * 0.4f + 0.2f) * 255.0f |> byte
        let color = Color (Color.iconSelected.R, Color.iconSelected.G, Color.iconSelected.B, alpha)
        node.Color <- color
        selectedTime <- selectedTime + Engine.DeltaSecond
    )

  let private mainButtons (model: Model) =
    [|for (path, mode) in modeButtons ->
        let texture = Texture2D.LoadStrict path

        Rectangle.Create(color = Color.iconBack, zOrder = ZOrder.iconBackground)
        |> BoxUI.withChildren [|
            yield
              Sprite.Create(
                keepAspect = true,
                zOrder = ZOrder.icon,
                texture = texture,
                color = Color.iconColor
              )
              |> BoxUI.margin (LengthScale.RelativeMin, 0.1f)
              :> Element

            if mode = model.cursor then
              selectedTime <- 0.0f
              yield
                Rectangle.Create(zOrder = ZOrder.iconSelected)
                |> BoxUI.onUpdate (selectedSpriteOnUpdate)
                :> Element
        |]
    |]

  let private mainMenu (model: Model) =
    Empty.Create()
    |> BoxUI.marginRight (LengthScale.Relative, 1.0f - mainMenuRatio)
    |> BoxUI.withChildren [|
      Empty.Create()
      |> BoxUI.marginX (LengthScale.Relative, 0.1f)
      |> BoxUI.marginTop (LengthScale.Relative, 0.08f)
      |> BoxUI.withChildren [|
        Grid.Create(200.0f) :> Element
        |> BoxUI.withChildren (mainButtons model)
        Rectangle.Create(zOrder = ZOrder.footer) :> Element
        |> BoxUI.marginTop (LengthScale.Relative, 0.8f)
      |]
    |]
    :> Element

  let private sideBar (model: Model) =
    Rectangle.Create(color = Color.sideBarColor, zOrder = ZOrder.sideMenuBackground)
    |> BoxUI.marginLeft (LengthScale.Relative, mainMenuRatio)
    :> Element

  let menu (model: Model) =
    Window.Create()
    |> BoxUI.withChildren [|
      Rectangle.Create(color = Color.backgroundColor, zOrder = ZOrder.background) :> Element
      mainMenu model
      sideBar model
    |]

open RouteTiles.Core.Types.Common

module MenuInput =
  open MenuCore

  let keyboard =
    [|
      for (key, _, _, dir) in InputControl.dirPairs -> [|key, ButtonState.Push|], Msg.MoveMode dir
    |]

  let joystick = [|
    for (_, btnL, _, dir) in InputControl.dirPairs -> (btnL, ButtonState.Push), Msg.MoveMode dir
  |]

open RouteTiles.Core.Utils

type Menu() =
  inherit Node()

  let updater = Updater<MenuCore.Model, MenuCore.Msg>()

  let coroutine = CoroutineNode()
  let uiRoot = BoxUIRootNode()

  do
    base.AddChildNode coroutine
    base.AddChildNode uiRoot

    updater.Subscribe(fun model ->
      uiRoot.ClearElement()
      uiRoot.SetElement <| MenuElement.menu model
    ) |> ignore

  do
    let getKeyboardInput = InputControl.getKeyboardInput MenuInput.keyboard
    let getJoystickInput = InputControl.getJoystickInput MenuInput.joystick

    coroutine.Add(seq {
      while true do
        getKeyboardInput ()
        |> Option.alt(fun () ->
          let count = Engine.Joystick.ConnectedJoystickCount
          seq {
            for i in 0..count-1 do
              match getJoystickInput i with
              | Some x -> yield x
              | _ -> ()
          }
          |> Seq.tryHead
        )
        |> Option.iter (updater.Dispatch >> ignore)

        yield()
    })

  override __.OnAdded() =
    updater.Init(MenuCore.initModel, MenuCore.update) |> ignore
