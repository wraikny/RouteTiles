module internal RouteTiles.App.MenuV2.MenuElement

open System
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core
open RouteTiles.Core.SubMenu
open RouteTiles.Core.Types
open RouteTiles.Core.Effects
open RouteTiles.App
open RouteTiles.App.BoxUIElements
open RouteTiles.App.MenuV2.ElementCommon

let leftArea() =
  FixedArea.Create(RectF(0.0f, 0.0f, 445.0f, 720.0f))
  :> Element

let rightArea() =
  FixedArea.Create(RectF(445.0f + 120.0f, 0.0f, 1280.0f - 445.0f - 120.0f, 720.0f))
  :> Element

let private createTitle (container: Container) =
  Sprite.Create
    ( aspect = Aspect.Fixed
    , texture = container.TitleTexture
    , zOrder = ZOrder.Menu.title
    )
  |> BoxUI.marginTop (LengthScale.Fixed, 80.0f)
  |> BoxUI.debug
  :> Element

let private buttonZOrders =
  {|button = ZOrder.Menu.buttonBackground
    buttonText = ZOrder.Menu.buttonText
  |}

let private createButtons (container: Container) (selections: string[]) param =
  leftArea()
  |> BoxUI.debug
  |> BoxUI.withChild (
    empty()
    |> BoxUI.marginBottom (LengthScale.Fixed, 80.0f)
    |> BoxUI.marginLeft (LengthScale.Fixed, 40.0f)
    |> BoxUI.withChild(
      buttons buttonZOrders container (32.0f, true) selections param
      |> BoxUI.alignX Align.Min
      |> BoxUI.alignY Align.Max
    )
    |> BoxUI.debug
  )

let private createDesc (container: Container) text =
  empty()
  |> BoxUI.marginBottom (LengthScale.Fixed, 80.0f)
  |> BoxUI.withChild(
    Text.Create
      ( text = text
      , font = container.Font
      , zOrder = ZOrder.Menu.description
      )
    |> BoxUI.alignY Align.Max
    |> BoxUI.debug
  )

let createCurrentMode zOrder (container: Container) (text: string) =
  empty ()
  |> BoxUI.marginLeft (LengthScale.Fixed, 40.0f)
  |> BoxUI.marginTop (LengthScale.Fixed, 40.0f)
  |> BoxUI.withChild (
    Text.Create
      ( text = text
      , font = container.Font
      , zOrder = zOrder
      )
    |> BoxUI.debug
  )

let createCurrentModeMenu = createCurrentMode ZOrder.Menu.currentMode

let private createMainMenu (container: Container) (state: ListSelector.State<MenuV2.Mode>) =
  [|
    createBackground container
    createButtons container container.MainMenuButtons (state.cursor, state.current)

    rightArea()
    |> BoxUI.withChildren [|
      createTitle container
      createDesc container container.MainMenuDescriptions.[state.cursor]
    |]
  |]

let private createGamemodeSelect (container: Container) (state: ListSelector.State<SoloGame.GameMode>) =
  [|
    createBackground container

    createCurrentModeMenu container container.TextMap.modes.gameModeSelect

    createButtons container container.GameModeButtons (state.cursor, state.current)
    rightArea().With(createDesc container container.GameModeDescriptions.[state.cursor])
  |]

let private createSetting (container: Container) (state: Setting.State) =
  state |> function
  | Setting.Base { selector = selector } ->
    [|
      createBackground container

      createCurrentModeMenu container container.TextMap.modes.setting

      createButtons container container.SettingMenuButtons (selector.cursor, selector.current)
      rightArea().With(createDesc container container.SettingModeDescriptions.[selector.cursor])
    |]
  | Setting.State.InputName _ ->
    failwithf "Invalid State: %A" state

  | _ -> Array.empty




let private createBlur zo1 zo2 =
  [|
    GaussianBlur.Create(intensity = 5.0f, zOrder = zo1) :> Element
    Rectangle.Create
      ( zOrder = zo2
      , color = Consts.Menu.blurDarkColor
      )
    :> Element
  |]

let createPause (container: Container) (state: ListSelector.State<MenuV2.PauseSelect>) =
  [|
    createBackground container

    createCurrentModeMenu container container.TextMap.modes.pause

    yield! createBlur ZOrder.Menu.blur ZOrder.Menu.darkMask
    createButtons container container.PauseModeButtons (state.cursor, state.current)
    rightArea().With(createDesc container container.PauseModeDescriptions.[state.cursor])
  |]

let private createControllerSelect =
  controllerSelect {| buttonZOrders with desc = ZOrder.Menu.description; background = ZOrder.Menu.frameBackground |}

let create (container: Container) (state: MenuV2.State) =
  createBase()
  |> BoxUI.withChildren (
    state |> function
    | MenuV2.State.MainMenuState (_, state) ->
      createMainMenu container state

    | MenuV2.State.GameModeSelectState (WithContext state, _) ->
      createGamemodeSelect container state

    | MenuV2.State.SettingMenuState (state, _) ->
      createSetting container state

    | MenuV2.State.ControllerSelectState (WithContext(MenuV2.ControllerSelectToPlay state), _) ->
      [|
        createBackground container
        createCurrentModeMenu container container.TextMap.modes.controllerSelect
        yield!
          createControllerSelect container state
      |]
    // | MenuV2.State.ControllerSelectState (WithContext state, _) ->
    //   [|
    //     createBackground container

    //     yield! createBlur ZOrder.Menu.blurOverGameInfo ZOrder.Menu.darkMaskOverGameInfo

    //     createCurrentMode ZOrder.Menu.currentModeOverGameInfo container container.TextMap.modes.controllerSelect

    //     yield!
    //       createControllerSelect container state.Value
    //   |]

    | MenuV2.State.PauseState(WithContext state, _) ->
      createPause container state

    | _ -> Array.empty
  )
