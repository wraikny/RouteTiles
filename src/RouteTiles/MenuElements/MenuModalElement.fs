module internal RouteTiles.App.Menu.MenuModalElement

open System
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Menu
open RouteTiles.Menu.SubMenu
open RouteTiles.Menu.Types
open RouteTiles.Menu.Effects
open RouteTiles.App
open RouteTiles.App.BoxUIElements
open RouteTiles.App.Menu.MenuElement
open RouteTiles.App.Menu.ElementCommon

let private createBlur () =
  [|
    GaussianBlur.Create(intensity = 5.0f, zOrder = ZOrder.MenuModal.blur) :> Element
    Rectangle.Create
      ( zOrder = ZOrder.MenuModal.darkMask
      , color = Consts.Menu.blurDarkColor
      )
    :> Element
  |]


// let private createModalWithChildren (container: Container) marginTop children =
//   modalFrame ZOrder.MenuModal.background container
//   |> BoxUI.withChild (
//     empty ()
//     |> BoxUI.debug
//     |> BoxUI.marginBottom (LengthScale.Fixed, 40.0f)
//     |> BoxUI.marginTop (LengthScale.Fixed, marginTop)
//     |> BoxUI.marginX (LengthScale.Fixed, 40.0f)
//     |> BoxUI.withChild (
//       ItemList.Create(itemMargin = 12.0f)
//       |> BoxUI.alignY Align.Min
//       |> BoxUI.withChildren children
//     )
//   )

let private modalText font fontSize color text =
  Text.Create
    ( font = font
    , fontSize = fontSize
    , text = text
    , color = Nullable(color)
    , zOrder = ZOrder.MenuModal.text
    )

let modalWithDescriptionAndChildren (container: Container) marginBottom (description: string) (children) =
  modalFrame ZOrder.MenuModal.background container
  |> BoxUI.withChild(
    empty ()
    |> BoxUI.debug
    |> BoxUI.marginTop (LengthScale.Fixed, 80.0f)
    |> BoxUI.marginBottom (LengthScale.Fixed, marginBottom)
    |> BoxUI.marginX (LengthScale.Fixed, 40.0f)
    |> BoxUI.withChild (
      modalText container.Font 32.0f (Color(255, 255, 255, 255)) description
      |> BoxUI.alignX Align.Center
      |> BoxUI.alignY Align.Min
      |> BoxUI.debug
    )
    |> BoxUI.withChildren children
  )

let private createTextsModal (container: Container) (description: string) alignX texts =
  modalWithDescriptionAndChildren container 80.0f description [|
    ItemList.Create(itemMargin = 12.0f)
    |> BoxUI.alignX Align.Center
    |> BoxUI.alignY Align.Max
    |> BoxUI.withChildren (
      texts |> Array.map(fun text ->
        modalText container.Font 32.0f (Color(255, 255, 255, 255)) text
        |> BoxUI.alignX alignX
        |> BoxUI.debug
      )
    )
  |]

let createInputUsernameModal (container: Container) (state: StringInput.State) =
  let frameSize = Vector2F(520.0f, 80.0f)

  modalWithDescriptionAndChildren
    container
    80.0f
    container.TextMap.descriptions.changeUsername
    [|
      Sprite.Create
        ( aspect = Aspect.Fixed
        , texture = container.InputFrame
        , src = Nullable(RectF(Vector2F(0.0f, 0.0f), frameSize))
        , zOrder = ZOrder.MenuModal.inputFrame
        )
      |> BoxUI.debug
      |> BoxUI.alignX Align.Center
      |> BoxUI.alignY Align.Max
      |> BoxUI.withChild (
        let startTime = Engine.Time
        Sprite.Create
          ( aspect = Aspect.Fixed
          , texture = container.InputFrame
          , src = Nullable(RectF(Vector2F(0.0f, frameSize.Y), frameSize))
          , zOrder = ZOrder.MenuModal.buttonBackground
          )
        |> BoxUI.onUpdate (fun node ->
          let dTime = Engine.Time - startTime

          let sinTime = cos (dTime * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)

          let a = (1.0f + sinTime) * 0.5f
          let (aMin, aMax) = Consts.Menu.cursorAlphaMinMax
          let alpha = (a * (aMax - aMin) + aMin) * 255.0f |> int
          node.Color <- Color (255, 255, 255, alpha)
        )
      )
      |> BoxUI.withChild(
        let text, color =
          if state.current = ""
          then container.TextMap.descriptions.namePlaceholder, Color(100uy, 100uy, 100uy)
          else state.current, Color(0uy, 0uy, 0uy)

        Text.Create
          ( font = container.Font
          , fontSize = 32.0f
          , text = text
          , color = Nullable(color)
          , zOrder = ZOrder.MenuModal.text
          )
        |> BoxUI.alignCenter
        |> BoxUI.debug
      )
    |]


let createVolumeSettingModal (container: Container) (state: VolumeSetting.State) =
  let createText color text =
    Text.Create
      ( font = container.Font
      , fontSize = 32.0f
      , text = text
      , color = Nullable(color)
      , zOrder = ZOrder.MenuModal.buttonText
      )
    |> BoxUI.alignY Align.Center

  modalWithDescriptionAndChildren
    container
    40.0f
    container.TextMap.descriptions.changeVolume
    [|
      ItemList.Create(itemMargin = 20.0f)
      |> BoxUI.alignX Align.Center
      |> BoxUI.alignY Align.Max
      |> BoxUI.withChildren (
        // let currentMode = VolumeSetting.VolumeMode.items.[state.cursor]
        VolumeSetting.VolumeMode.items
        |> Array.mapi (fun index mode ->
          let elem = twoSplitFrame (ZOrder.MenuModal.buttonBackground) container

          let textElem =
            createText (Color(255, 255, 255, 255)) container.VolumeModeButtons.[index]
            

          if index = state.cursor then
            elem.AddChild(
              twoSplitFrameHighlight (ZOrder.MenuModal.buttonBackground + 1) container
              |> BoxUI.onUpdate (createHighlightUpdate null textElem)
              :> Element
            )

          let volume =
            mode |> function
            | VolumeSetting.VolumeMode.BGM -> state.bgmVolume
            | VolumeSetting.VolumeMode.SE -> state.seVolume

          let lineLength = 120.0f
          let cursorWidth = 4.0f
          
          elem
          |> BoxUI.withChildren [|
            empty ()
            |> BoxUI.marginLeft (LengthScale.Fixed, twoSplitFrameXMargin)
            |> BoxUI.marginRight (LengthScale.Fixed, twoSplitFrameXMargin)
            |> BoxUI.withChildren [|
              textElem

              createText (Color(0, 0, 0, 255)) (sprintf "%d" volume)
              |> BoxUI.alignX Align.Max
            |]

            empty ()
            |> BoxUI.marginLeft (LengthScale.Fixed, gameInfoFrameSize.X * 0.5f + twoSplitFrameXMargin)
            |> BoxUI.debug
            |> BoxUI.withChild (
              FixedWidth.Create(lineLength)
              |> BoxUI.withChildren [|
                FixedHeight.Create(2.0f)
                |> BoxUI.alignY Align.Center
                |> BoxUI.withChild (
                  Rectangle.Create
                    ( color = Nullable(Color(0, 0, 0, 255))
                    , zOrder = ZOrder.MenuModal.buttonText
                    )
                )
                :> Element

                empty()
                |> BoxUI.marginLeft (LengthScale.Fixed, (lineLength - cursorWidth) * VolumeSetting.volumeIntToFloat32 volume)
                |> BoxUI.withChild (
                  FixedSize.Create(Vector2F(4.0f, 8.0f))
                  |> BoxUI.alignY Align.Center
                  |> BoxUI.withChild (
                    Rectangle.Create
                      ( color = Nullable(Color(0, 0, 0, 255))
                      , zOrder = ZOrder.MenuModal.buttonText
                      )
                  )
                )
              |]
            )
          |]
        )
      )
    |]

let private listSelectorZOrders =
  {|button = ZOrder.MenuModal.buttonBackground
    buttonText = ZOrder.MenuModal.buttonText
    desc = ZOrder.MenuModal.description
    background = ZOrder.MenuModal.background
  |}

let createControllerSelect =
  controllerSelect listSelectorZOrders


let private createCurrentMode = createCurrentMode ZOrder.MenuModal.currentMode

let private createWaitingResponse (container: Container) =
  [|
    yield! createBlur ()
    modalWithDescriptionAndChildren
      container 120.0f
      container.TextMap.descriptions.waitingResponse
      Array.empty
  |]

let rec private errorToMessage (error: exn) =
  error |> function
  | :? AggregateException as e ->
    [|
      for e in e.Flatten().InnerExceptions -> errorToMessage e
    |]
    |> String.concat "\n\n"
  | :? Net.Http.HttpRequestException ->
    "サーバーとの通信に失敗しました"
  | e ->
    sprintf "%s\n%s" (e.GetType().Name) e.Message

let private createErrorModal (container: Container) (error: exn) =
  let white = Color(255, 255, 255, 255)
  modalWithDescriptionAndChildren
    container 80.0f
    container.TextMap.descriptions.error
    [|
      empty ()
      |> BoxUI.marginTop (LengthScale.Fixed, 120.0f)
      |> BoxUI.withChild (
        errorToMessage error
        |> modalText container.ErrorMessageFont 32.0f white
        |> BoxUI.alignX Align.Center
      )
    |]


let createModal (container: Container) (state: Menu.State) =
  let tm = container.TextMap

  state |> function
    | Menu.State.HowToState (SinglePage.SinglePageState state, _) ->
      ValueSome [|
        yield! createBlur()
        yield createCurrentMode container tm.modes.howTo

        let f x = centeredSprite ZOrder.MenuModal.background x

        let elem = state |> function
          | Menu.HowToMode.KeyboardShift -> f container.HowToKeyboardShift
          | Menu.HowToMode.KeyboardSeparate -> f container.HowToKeyboardSeparate
          | Menu.HowToMode.Joystick -> f container.HowToJoystick
          | Menu.HowToMode.Slide -> f container.HowToSlide
          | Menu.HowToMode.Route -> f container.HowToRoute
          | Menu.HowToMode.Loop -> f container.HowToLoop
          | Menu.HowToMode.Game -> f container.HowToGame
          | Menu.HowToMode.Point -> f container.HowToPoint
          | Menu.HowToMode.Board -> f container.HowToBoard

        yield elem
      |]

    | Menu.State.SettingMenuState (Setting.State.InputName(state, _), _) ->
      ValueSome [|
          yield! createBlur ()
          createCurrentMode container tm.modes.nameSetting
          createInputUsernameModal container state
      |]

    | Menu.State.SettingMenuState (Setting.State.Volume(state, _), _) ->
      ValueSome [|
        yield! createBlur ()
        createCurrentMode container tm.modes.volumeSetting
        createVolumeSettingModal container state
      |]

    | Menu.State.SettingMenuState (Setting.State.Background(state, _), _) ->
      ValueSome [|
        PostEffect.BackgroundElement.Create
          ( Helper.PostEffect.toPath Background.items.[state.cursor]
          , zOrder = ZOrder.MenuModal.postEffectToShow
          )
        :> Element

        createCurrentMode container tm.modes.volumeSetting

        yield!
          listSelectorModal
            listSelectorZOrders
            container
            tm.descriptions.selectBackground
            (container.Font, 32.0f)
            state
            container.BackgroundButtons
      |]

    | Menu.State.ControllerSelectState (WithContext(Menu.ControllerSelectToPlay _), _) -> ValueNone
    | Menu.State.PauseState(Pause.ControllerSelectState (state, _), _)
    | Menu.State.ControllerSelectState (WithContext (Menu.ControllerSelectToPlay state), _)
    | Menu.State.ControllerSelectState (WithContext (Menu.ControllerSelectWhenRejected state), _) ->
      [|
        createBackground container

        yield! createBlur () // ZOrder.Menu.blurOverGameInfo ZOrder.Menu.darkMaskOverGameInfo

        createCurrentMode container tm.modes.controllerSelect

        yield! createControllerSelect container state
      |]
      |> ValueSome

    | Menu.State.WaitingResponseState _ ->
      ValueSome <| createWaitingResponse container

    | Menu.State.ErrorViewState(SinglePage.SinglePageState(error), _) ->
      ValueSome [|
          // createBackground container
          yield! createBlur ()
          createErrorModal container error
        |]

    | Menu.State.GameResultState(resultState, _) ->
      resultState |> function
      | GameResult.WaitingResponseState _ ->
        ValueSome <| createWaitingResponse container

      | GameResult.ErrorViewState(SinglePage.SinglePageState error, _) ->
        ValueSome [|
          // createBackground container
          yield! createBlur ()
          createErrorModal container error
        |]

      | GameResult.InputName (state, _) ->
        ValueSome <| [|
          yield! createBlur ()
          createInputUsernameModal container state
        |]

      | _ -> ValueNone

    | _ -> ValueNone
  |> ValueOption.map(fun elems ->
    createBase ()
      |> BoxUI.withChildren elems
  )
