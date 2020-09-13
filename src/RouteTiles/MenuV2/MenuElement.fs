module RouteTiles.App.MenuElement

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

type Container (textMap: TextMap.TextMap) =
  member val TextMap = textMap
  member val BackgroundTexture = Texture2D.LoadStrict(@"Menu/background_dark.png")
  member val MaskTexture = Texture2D.LoadStrict(@"Menu/background_mask.png")
  member val TitleTexture = Texture2D.LoadStrict(@"Menu/title.png")
  member val ButtonBackground = Texture2D.LoadStrict(@"Menu/button-metalic-dark-highlight-320x80.png")
  member val InputUsernameBackground = Texture2D.LoadStrict(@"Menu/input_username.png")
  // member val Font = Font.LoadStaticFontStrict(@"Font/Makinas-4-Square-32/font.a2f")
  member val Font = Font.LoadDynamicFontStrict(@"mplus-1c-bold.ttf", 32)

  member val MainMenuButtons =
    MenuV2.Mode.items |> Array.map (function
      | MenuV2.Mode.GamePlay -> textMap.buttons.play
      | MenuV2.Mode.Ranking -> textMap.buttons.ranking
      | MenuV2.Mode.Setting -> textMap.buttons.setting
    )

  member val MainMenuDescriptions =
    MenuV2.Mode.items |> Array.map (function
      | MenuV2.Mode.GamePlay -> textMap.descriptions.play
      | MenuV2.Mode.Ranking -> textMap.descriptions.ranking
      | MenuV2.Mode.Setting -> textMap.descriptions.setting
    )

  member val GameModeButtons =
    MenuV2.GameMode.items |> Array.map(function
      | MenuV2.GameMode.TimeAttack2000 -> textMap.buttons.timeattack2000
      | MenuV2.GameMode.ScoreAttack180 -> textMap.buttons.scoreattack180
    )

  member val GameModeDescriptions =
    MenuV2.GameMode.items |> Array.map (function
    | MenuV2.GameMode.TimeAttack2000 -> textMap.descriptions.timeattack2000
    | MenuV2.GameMode.ScoreAttack180 -> textMap.descriptions.scoreattack180
  )

  member val SettingMenuButtons =
    Setting.Mode.items |> Array.map (function
      | Setting.Mode.InputName -> textMap.buttons.namesetting
      | Setting.Mode.Background -> textMap.buttons.backgroundsetting
      | Setting.Mode.Enter -> textMap.buttons.save
    )

  member val SettingModeDescriptions =
    Setting.Mode.items |> Array.map (function
      | Setting.Mode.InputName -> textMap.descriptions.namesetting
      | Setting.Mode.Background -> textMap.descriptions.backgroundsetting
      | Setting.Mode.Enter -> textMap.descriptions.settingsave
    )

  member val PauseModeButtons =
    MenuV2.PauseSelect.items |> Array.map (function
      | MenuV2.Continue -> textMap.buttons.continueGame
      | MenuV2.ChangeController -> textMap.buttons.changeController
      | MenuV2.Restart -> textMap.buttons.restartGame
      | MenuV2.Quit -> textMap.buttons.quitGame
    )

  member val PauseModeDescriptions =
    MenuV2.PauseSelect.items |> Array.map (function
      | MenuV2.Continue -> textMap.descriptions.continueGame
      | MenuV2.ChangeController -> textMap.descriptions.changeController
      | MenuV2.Restart -> textMap.descriptions.restartGame
      | MenuV2.Quit -> textMap.descriptions.quitGame
    )



let empty() = Empty.Create() :> Element

let private fixedArea(pos, size) =
  FixedArea.Create(RectF(pos, size))
  :> ElementRoot

let leftArea() =
  FixedArea.Create(RectF(0.0f, 0.0f, 445.0f, 720.0f))
  :> Element

let rightArea() =
  FixedArea.Create(RectF(445.0f + 120.0f, 0.0f, 1280.0f - 445.0f - 120.0f, 720.0f))
  :> Element


let buttons (container: Container) (selections: string[]) (cursor: int, current: int option) =
  let size = Vector2F(320.0f, 80.0f)

  let current = current |> Option.defaultValue -1

  ItemList.Create(itemMargin = 40.0f)
  |> BoxUI.withChildren (
    selections
    |> Array.mapi (fun index text ->
      let srcPos =
        if index = current && index <> cursor then Vector2F(0.0f, 2.0f * size.Y)
        else Vector2F()

      let baseElem = FixedSize.Create(size)

      baseElem
      // |> BoxUI.debug
      // |> BoxUI.marginLeft (LengthScale.Fixed, 40.0f)
      |> BoxUI.withChildren [|
        let textElem =
          Text.Create(font = container.Font, text = text, zOrder = ZOrder.Menu.buttonText)
          |> BoxUI.alignCenter

        yield
          Sprite.Create
            ( aspect = Aspect.Fixed
            , texture = container.ButtonBackground
            , zOrder = ZOrder.Menu.buttonBackground
            , src = Nullable(RectF(srcPos, size))
            )
          |> BoxUI.withChild textElem
          :> Element

        if cursor = index then
          let startTime = Engine.Time
          yield (
            Sprite.Create
              ( aspect = Aspect.Fixed
              , texture = container.ButtonBackground
              , zOrder = ZOrder.Menu.buttonHighlight
              , src = Nullable(RectF(Vector2F(size.X, 1.0f * size.Y), size))
              )
            |> BoxUI.onUpdate (fun node ->
              let dTime = Engine.Time - startTime

              if dTime < Consts.Menu.offsetAnimationPeriod then
                let a = Easing.GetEasing(EasingType.OutQuad, dTime / Consts.Menu.offsetAnimationPeriod)
                baseElem.MarginLeft <- (LengthScale.Fixed, 20.0f * a)

              let sinTime = cos (dTime * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)

              let a = (1.0f + sinTime) * 0.5f
              let (aMin, aMax) = Consts.Menu.cursorAlphaMinMax
              let alpha = (a * (aMax - aMin) + aMin) * 255.0f |> int
              node.Color <- Color (255, 255, 255, alpha)
              textElem.Node.Color <- Color (255, 255, int (100.0f + 155.0f * (1.0f - a)), 255)
            )
            :> Element
          )
      |]
      )
  )
  :> Element

let createBackground (container: Container) =
  empty()
  |> BoxUI.withChildren [|
    Sprite.Create
      ( aspect = Aspect.Fixed
      , texture = container.BackgroundTexture
      , zOrder = ZOrder.Menu.background
      )
    Sprite.Create
      ( aspect = Aspect.Fixed
      , texture = container.MaskTexture
      , zOrder = ZOrder.Menu.backgroundMask
      )
  |]

let createTitle (container: Container) =
  Sprite.Create
    ( aspect = Aspect.Fixed
    , texture = container.TitleTexture
    , zOrder = ZOrder.Menu.title
    )
  |> BoxUI.marginTop (LengthScale.Fixed, 80.0f)
  |> BoxUI.debug
  :> Element

let createButtons (container: Container) (selections: string[]) param =
  let size = Vector2F(320.0f, float32 selections.Length * 80.0f + float32 (selections.Length - 1) * 40.0f)
  leftArea()
  |> BoxUI.withChild (
    empty()
    |> BoxUI.marginBottom (LengthScale.Fixed, 80.0f)
    |> BoxUI.withChild(
      FixedSize.Create(size)
      |> BoxUI.alignX Align.Center
      |> BoxUI.alignY Align.Max
      |> BoxUI.withChild(
        buttons container selections param
      )
    )
    // |> BoxUI.debug
  )

let createDesc (container: Container) text =
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

let createMainMenu (container: Container) (state: ListSelector.State<MenuV2.Mode>) =
  [|
    createBackground container
    createButtons container container.MainMenuButtons (state.cursor, state.current)

    rightArea()
    |> BoxUI.withChildren [|
      createTitle container
      createDesc container container.MainMenuDescriptions.[state.cursor]
    |]
  |]

let createGamemodeSelect (container: Container) (state: ListSelector.State<MenuV2.GameMode>) =
  [|
    createBackground container
    createButtons container container.GameModeButtons (state.cursor, state.current)
    rightArea().With(createDesc container container.GameModeDescriptions.[state.cursor])
  |]

let createSetting (container: Container) (state: Setting.State) =
  state |> function
  | Setting.Base { selector = selector } ->
    [|
      createBackground container
      createButtons container container.SettingMenuButtons (selector.cursor, selector.current)
      rightArea().With(createDesc container container.SettingModeDescriptions.[selector.cursor])
    |]
  | Setting.State.InputName (state, _) ->
    [|
      // todo: blur

      Sprite.Create
        ( aspect = Aspect.Fixed
        , texture = container.InputUsernameBackground
        )
      |> BoxUI.alignCenter
      :> Element
      |> BoxUI.withChild(
        empty()
        |> BoxUI.debug
        |> BoxUI.marginBottom (LengthScale.Fixed, 80.0f)
        |> BoxUI.withChild(
          let text, color =
            if state.current = ""
            then "username", Color(100uy, 100uy, 100uy)
            else state.current, Color(0uy, 0uy, 0uy)

          Text.Create
            ( font = container.Font
            , text = text
            , color = Nullable(color)
            )
          |> BoxUI.alignX Align.Center
          |> BoxUI.alignY Align.Max
          |> BoxUI.debug
        )
      )
    |]
  | _ -> Array.empty


let createControllerSelect (container: Container) (MenuV2.ControllerSelect (_, state)) =
  let controllers =
    state.selection
    |> Array.map(function
      | Controller.Keyboard -> "キーボード"
      | Controller.Joystick(_, name, _) -> name
    )
  [|
    // createBackground container
    // todo: blur
    createButtons container controllers (state.cursor, state.current)
    rightArea().With(createDesc container (container.TextMap.descriptions.selectController))
  |]

let createPause (container: Container) (state: ListSelector.State<MenuV2.PauseSelect>) =
  [|
    // createBackground container
    // todo: blur
    createButtons container container.PauseModeButtons (state.cursor, state.current)
    rightArea().With(createDesc container container.PauseModeDescriptions.[state.cursor])
  |]



let create (container: Container) (state: MenuV2.State) =
  fixedArea(Vector2F(), Consts.ViewCommon.windowSize.To2F())
  |> BoxUI.withChildren (
    state |> function
    | MenuV2.State.MainMenuState (_, state) ->
      createMainMenu container state

    | MenuV2.State.GameModeSelectState (WithContext state, _) ->
      createGamemodeSelect container state

    | MenuV2.State.SettingMenuState (state, _) ->
      createSetting container state

    | MenuV2.State.ControllerSelectState(state, _) ->
      createControllerSelect container state

    | MenuV2.State.PauseState(WithContext state, _) ->
      createPause container state

    | _ -> Array.empty
  )
