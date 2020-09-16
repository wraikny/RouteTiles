module RouteTiles.App.MenuV2.MenuModalElement

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

let createInputUsernameModal (container: Container) (state: StringInput.State) =
  let frameSize = Vector2F(520.0f, 80.0f)

  [|
    GaussianBlur.Create(intensity = 5.0f, zOrder = ZOrder.MenuModal.blur) :> Element
    Sprite.Create
      ( aspect = Aspect.Fixed
      , texture = container.InputBackground
      , zOrder = ZOrder.MenuModal.background
      )
    |> BoxUI.alignCenter
    :> Element
    |> BoxUI.withChild(
      empty ()
      |> BoxUI.debug
      |> BoxUI.marginBottom (LengthScale.Fixed, 40.0f)
      |> BoxUI.marginTop (LengthScale.Fixed, 120.0f)
      |> BoxUI.withChild (
        Text.Create
          ( font = container.Font
          , text = container.TextMap.descriptions.changeUsername
          , color = Nullable(Color(0uy, 0uy, 0uy))
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
          , zOrder = ZOrder.MenuModal.frame
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
            , zOrder = ZOrder.MenuModal.frame
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
          empty ()
          |> BoxUI.marginBottom (LengthScale.Fixed, 20.0f)
          |> BoxUI.withChild (
            let text, color =
              if state.current = ""
              then "username", Color(100uy, 100uy, 100uy)
              else state.current, Color(0uy, 0uy, 0uy)

            Text.Create
              ( font = container.Font
              , text = text
              , color = Nullable(color)
              , zOrder = ZOrder.MenuModal.text
              )
            |> BoxUI.alignX Align.Center
            |> BoxUI.alignY Align.Max
            |> BoxUI.debug
          )
        )
      )
    )
  |]


let createModal (container: Container) (state: MenuV2.State) =
  state |> function
    | MenuV2.State.SettingMenuState (Setting.State.InputName(state, _), _) ->
      createBase ()
      |> BoxUI.withChildren (
          createInputUsernameModal container state
      )
      |> ValueSome

    | MenuV2.State.ControllerSelectState (MenuV2.ControllerSelectWhenRejected state, _) ->
      createBase ()
      |> BoxUI.withChild (
        GaussianBlur.Create(intensity = 5.0f, zOrder = ZOrder.MenuModal.blur) :> Element
        empty()
      )
      |> BoxUI.withChildren (
        createControllerSelect container state
      )
      |> ValueSome

    | _ -> ValueNone
