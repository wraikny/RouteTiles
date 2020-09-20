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

let createInputUsernameModal (container: Container) (state: StringInput.State) =
  let frameSize = Vector2F(520.0f, 80.0f)

  [|
    modalFrame ZOrder.MenuModal.background container
    |> BoxUI.withChild(
      empty ()
      |> BoxUI.debug
      |> BoxUI.marginBottom (LengthScale.Fixed, 40.0f)
      |> BoxUI.marginTop (LengthScale.Fixed, 120.0f)
      |> BoxUI.withChild (
        Text.Create
          ( font = container.Font
          , text = container.TextMap.descriptions.changeUsername
          , color = Nullable(Color(255uy, 255uy, 255uy))
          , zOrder = ZOrder.MenuModal.text
          )
        |> BoxUI.alignX Align.Center
        |> BoxUI.alignY Align.Min
        |> BoxUI.debug
      )
      |> BoxUI.withChild (
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
            , zOrder = ZOrder.MenuModal.inputFrame + 1
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
      [|
          yield! createBlur ()
          createCurrentMode container container.TextMap.modes.nameSetting
          yield! createInputUsernameModal container state
      |]
      |> ValueSome

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
          yield! createInputUsernameModal container state
        |]

      | _ -> ValueNone

    | _ -> ValueNone
  |> ValueOption.map(fun elems ->
    createBase ()
      |> BoxUI.withChildren elems
  )
