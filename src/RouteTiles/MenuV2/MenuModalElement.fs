module internal RouteTiles.App.MenuV2.MenuModalElement

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
open RouteTiles.App.MenuV2.MenuElement
open RouteTiles.App.MenuV2.ElementCommon

let private createBlur () =
  [|
    GaussianBlur.Create(intensity = 5.0f, zOrder = ZOrder.MenuModal.blur) :> Element
    Rectangle.Create
      ( zOrder = ZOrder.MenuModal.darkMask
      , color = Consts.Menu.blurDarkColor
      )
    :> Element
  |]


let private createTextModal (container: Container) text =
  modalFrame ZOrder.MenuModal.background container
  |> BoxUI.withChild (
    empty ()
    |> BoxUI.debug
    |> BoxUI.marginBottom (LengthScale.Fixed, 40.0f)
    |> BoxUI.marginTop (LengthScale.Fixed, 120.0f)
    |> BoxUI.withChild (
      Text.Create
        ( font = container.Font
        , text = text
        , color = Nullable(Color(255uy, 255uy, 255uy))
        , zOrder = ZOrder.MenuModal.text
        )
      |> BoxUI.alignX Align.Center
      |> BoxUI.alignY Align.Min
      |> BoxUI.debug
    )
  )

let modalWithDescriptionAndChildren (container: Container) marginBottom (description: string) (children) =
  // let frameSize = Vector2F(520.0f, 80.0f)

  modalFrame ZOrder.MenuModal.background container
  |> BoxUI.withChild(
    empty ()
    |> BoxUI.debug
    |> BoxUI.marginBottom (LengthScale.Fixed, marginBottom)
    |> BoxUI.marginTop (LengthScale.Fixed, 80.0f)
    |> BoxUI.withChild (
      Text.Create
        ( font = container.Font
        , text = description
        , color = Nullable(Color(255uy, 255uy, 255uy))
        , zOrder = ZOrder.MenuModal.text
        )
      |> BoxUI.alignX Align.Center
      |> BoxUI.alignY Align.Min
      |> BoxUI.debug
    )
    |> BoxUI.withChildren children
  )

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
              |> fun e ->
                BoxUISystem.Post(fun () ->
                  e.add_OnUpdateEvent (createHighlightUpdate null textElem)
                )
                e
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


let createControllerSelect =
  controllerSelect
    {|button = ZOrder.MenuModal.buttonBackground
      buttonText = ZOrder.MenuModal.buttonText
      desc = ZOrder.MenuModal.description
      background = ZOrder.MenuModal.background
    |}


let private createCurrentMode = createCurrentMode ZOrder.MenuModal.currentMode

let private createWaitingResponse (container: Container) =
  [|
    yield! createBlur ()
    createTextModal container container.TextMap.descriptions.waitingResponse
  |]



let createModal (container: Container) (state: MenuV2.State) =
  state |> function
    | MenuV2.State.SettingMenuState (Setting.State.InputName(state, _), _) ->
      ValueSome [|
          yield! createBlur ()
          createCurrentMode container container.TextMap.modes.nameSetting
          createInputUsernameModal container state
      |]

    | MenuV2.State.SettingMenuState (Setting.State.Volume(state, _), _) ->
      ValueSome [|
        yield! createBlur ()
        createCurrentMode container container.TextMap.modes.volumeSetting
        createVolumeSettingModal container state
      |]

    | MenuV2.State.ControllerSelectState (WithContext(MenuV2.ControllerSelectToPlay _), _) -> ValueNone

    | MenuV2.State.PauseState(Pause.ControllerSelectState (state, _), _)
    | MenuV2.State.ControllerSelectState (WithContext (MenuV2.ControllerSelectToPlay state), _)
    | MenuV2.State.ControllerSelectState (WithContext (MenuV2.ControllerSelectWhenRejected state), _) ->
      [|
        createBackground container

        yield! createBlur () // ZOrder.Menu.blurOverGameInfo ZOrder.Menu.darkMaskOverGameInfo

        createCurrentMode container container.TextMap.modes.controllerSelect

        yield! createControllerSelect container state
      |]
      |> ValueSome

    | MenuV2.State.WaitingResponseState _ ->
      ValueSome <| createWaitingResponse container

    | MenuV2.State.ErrorViewState(SinglePage.SinglePageState(error), _) ->
      ValueSome [|
          // createBackground container
          yield! createBlur ()
          createTextModal container (error.Message)
        |]

    | MenuV2.State.GameResultState(resultState, _) ->
      resultState |> function
      | GameResult.WaitingResponseState _ ->
        ValueSome <| createWaitingResponse container

      | GameResult.RankingListViewState(SinglePage.SinglePageState (_, _, Error error), _) ->
        ValueSome [|
          // createBackground container
          yield! createBlur ()
          createTextModal container (error.Message)
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
