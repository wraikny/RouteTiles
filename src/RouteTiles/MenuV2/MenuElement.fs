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

let empty() = Empty.Create() :> Element

let private fixedArea(pos, size) =
  FixedArea.Create(RectF(pos, size))
  :> ElementRoot

let createBase() = fixedArea(Vector2F(), Consts.ViewCommon.windowSize.To2F())

let leftArea() =
  FixedArea.Create(RectF(0.0f, 0.0f, 445.0f, 720.0f))
  :> Element

let rightArea() =
  FixedArea.Create(RectF(445.0f + 120.0f, 0.0f, 1280.0f - 445.0f - 120.0f, 720.0f))
  :> Element

let buttonSize = Vector2F(320.0f, 80.0f)

let buttons (container: Container) (itemMargin: float32, enabledCursorAnimation: bool) (selections: string[]) (cursor: int, current: int option) =
  let current = current |> Option.defaultValue -1

  ItemList.Create(itemMargin = itemMargin)
  |> BoxUI.withChildren (
    selections
    |> Array.mapi (fun index text ->
      let srcPos =
        if index = current && index <> cursor then Vector2F(0.0f, 2.0f * buttonSize.Y)
        else Vector2F()

      let baseElem = FixedSize.Create(buttonSize)

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
            , src = Nullable(RectF(srcPos, buttonSize))
            )
          |> BoxUI.withChild textElem
          :> Element

        if cursor = index then
          
          yield (
            Sprite.Create
              ( aspect = Aspect.Fixed
              , texture = container.ButtonBackground
              , zOrder = ZOrder.Menu.buttonHighlight
              , src = Nullable(RectF(Vector2F(buttonSize.X, 1.0f * buttonSize.Y), buttonSize))
              )
            |> fun e ->
              async {
                do! Async.Sleep 1
                let startTime = Engine.Time
                e.add_OnUpdateEvent (fun node ->
                  let dTime = Engine.Time - startTime
                  
                  let mutable finished = false
                  if enabledCursorAnimation && not finished then
                    if dTime < Consts.Menu.offsetAnimationPeriod then
                      let a = Easing.GetEasing(EasingType.OutQuad, dTime / Consts.Menu.offsetAnimationPeriod)
                      baseElem.MarginLeft <- (LengthScale.Fixed, 20.0f * a)
                    else
                      baseElem.MarginLeft <- (LengthScale.Fixed, 20.0f)
                      finished <- true

                  let sinTime = cos (dTime * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)

                  let a = (1.0f + sinTime) * 0.5f
                  let (aMin, aMax) = Consts.Menu.cursorAlphaMinMax
                  let alpha = (a * (aMax - aMin) + aMin) * 255.0f |> int
                  node.Color <- Color (255, 255, 255, alpha)
                  textElem.Node.Color <- Color (255, 255, int (100.0f + 155.0f * (1.0f - a)), 255)
                )
              }
              |> Async.StartImmediate

              e
            :> Element
          )
      |]
      )
  )

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
  leftArea()
  |> BoxUI.withChild (
    empty()
    |> BoxUI.marginBottom (LengthScale.Fixed, 80.0f)
    |> BoxUI.withChild(
      buttons container (32.0f, true) selections param
      |> BoxUI.alignX Align.Center
      |> BoxUI.alignY Align.Max
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

let createGamemodeSelect (container: Container) (state: ListSelector.State<SoloGame.GameMode>) =
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
  | Setting.State.InputName _ ->
    failwithf "Invalid State: %A" state

  | _ -> Array.empty


let createControllerSelect (container: Container) (state: ListSelector.State<Controller>) =
  let controllers =
    state.selection
    |> Array.map(function
      | Controller.Keyboard -> "キーボード"
      | Controller.Joystick(_, name, _) -> name
    )

  let current = state.current |> Option.filter((=) state.cursor) |> Option.map(fun _ -> state.cursor)

  let createArrow (isUp: bool) (drawn: bool) =
    let size = Vector2F(24.0f, 24.0f)

    let elem = FixedSize.Create(size) :> Element


    if drawn then
      elem.AddChild(
        Sprite.Create
          ( aspect = Aspect.Fixed
          , texture = container.SelectionArrow
          // , verticalFlip = flip
          , src = System.Nullable (RectF((if isUp then size.X else 0.0f), 0.0f, size.X, size.Y))
          , zOrder = ZOrder.Menu.buttonBackground
          )
        |> BoxUI.alignCenter
      )

    elem
    |> BoxUI.debug

  [|
    Sprite.Create
      ( aspect = Aspect.Fixed
      , texture = container.InputBackground
      , zOrder = ZOrder.Menu.frameBackground
      )
    :> Element
    |> BoxUI.alignCenter
    |> BoxUI.withChild (
      ItemList.Create()
      |> BoxUI.withChildren ([|
        FixedHeight.Create(60.0f) :> Element

        Text.Create
          ( text = container.TextMap.descriptions.selectController
          , font = container.Font
          , zOrder = ZOrder.Menu.description
          )
        :> Element
        
        FixedHeight.Create(40.0f) :> Element

        createArrow true (state.cursor > 0)

        FixedHeight.Create(10.0f) :> Element

        buttons container (0.0f, false) [| controllers.[state.cursor] |] (0, current)
        :> Element

        FixedHeight.Create(10.0f) :> Element

        createArrow false (state.cursor < state.selection.Length - 1)
      |]|> Array.map (BoxUI.alignX Align.Center))
    )

    // rightArea().With(createDesc container (container.TextMap.descriptions.selectController))
  |]


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
    yield! createBlur ZOrder.Menu.blur ZOrder.Menu.darkMask
    createButtons container container.PauseModeButtons (state.cursor, state.current)
    rightArea().With(createDesc container container.PauseModeDescriptions.[state.cursor])
  |]



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
        yield!
          createControllerSelect container state
      |]
    | MenuV2.State.ControllerSelectState (WithContext state, _) ->
      [|
        createBackground container
        yield! createBlur  ZOrder.Menu.blurOverGameInfo ZOrder.Menu.darkMaskOverGameInfo
        yield!
          createControllerSelect container state.Value
      |]

    | MenuV2.State.PauseState(WithContext state, _) ->
      createPause container state

    | _ -> Array.empty
  )
