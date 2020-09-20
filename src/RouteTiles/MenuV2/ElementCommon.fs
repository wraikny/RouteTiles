module internal RouteTiles.App.MenuV2.ElementCommon

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

let secondToDisplayTime second =
  sprintf "%02i:%02i.%03i"
    (second / 60.0f |> int)
    (second % 60.0f |> int)
    ((second % 1.0f) * 1000.0f |> int)

let empty() = Empty.Create() :> Element

let private fixedArea(pos, size) =
  FixedArea.Create(RectF(pos, size))
  :> ElementRoot

let createBase() = fixedArea(Vector2F(), Consts.ViewCommon.windowSize.To2F())

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

let buttonSize = Vector2F(320.0f, 80.0f)

let buttons
  (zOrders: {| button: int; buttonText: int |})
  (itemMargin: float32, enabledCursorAnimation: bool, transculentUncursored: bool)
  (container: Container)
  (selections: string[])
  (cursor: int, current: int option) =

  let current = current |> Option.defaultValue -1

  ItemList.Create(itemMargin = itemMargin)
  |> BoxUI.withChildren (
    selections
    |> Array.mapi (fun index text ->
      let alpha =
        if transculentUncursored && (cursor <> index) then
          150
        else
          255

      let srcPos, textColor =
        if index = current && index <> cursor then
          Vector2F(0.0f, 2.0f * buttonSize.Y), Color(100, 100, 255, alpha)
        else
          Vector2F(), Color(255, 255, 255, alpha)


      let baseElem = FixedSize.Create(buttonSize)

      baseElem
      // |> BoxUI.debug
      // |> BoxUI.marginLeft (LengthScale.Fixed, 40.0f)
      |> BoxUI.withChildren [|
        let textElem =
          Text.Create
            ( font = container.Font
            , text = text
            , zOrder =  zOrders.buttonText
            , color = Nullable(textColor)
          )
          |> BoxUI.alignCenter

        yield
          Sprite.Create
            ( aspect = Aspect.Fixed
            , texture = container.ButtonBackground
            , zOrder = zOrders.button
            , src = Nullable(RectF(srcPos, buttonSize))
            , color = Nullable(Color(255, 255, 255, alpha))
            )
          |> BoxUI.withChild textElem
          :> Element

        if cursor = index then
          yield (
            Sprite.Create
              ( aspect = Aspect.Fixed
              , texture = container.ButtonBackground
              , zOrder = zOrders.button + 1
              , src = Nullable(RectF(Vector2F(buttonSize.X, 1.0f * buttonSize.Y), buttonSize))
              )
            |> fun e ->
              BoxUISystem.Post(fun () ->
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
              )

              e
            :> Element
          )
      |]
      )
  )

let modalFrame zOrder (container: Container) =
  Sprite.Create
    ( aspect = Aspect.Fixed
    , texture = container.InputBackground
    , zOrder = zOrder
    )
  |> BoxUI.alignCenter
  :> Element


let controllerSelect (zOrders: {| button: int; buttonText: int; desc: int; background: int |}) (container: Container) (state: ListSelector.State<Controller>) =
  let controllers =
    state.selection
    |> Array.map(function
      | Controller.Keyboard -> container.TextMap.buttons.keyboard
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
          , src = Nullable (RectF((if isUp then size.X else 0.0f), 0.0f, size.X, size.Y))
          , zOrder = zOrders.button
          )
        |> BoxUI.alignCenter
      )

    elem
    |> BoxUI.debug

  [|
    modalFrame zOrders.background container
    |> BoxUI.withChild (
      ItemList.Create()
      |> BoxUI.withChildren (
        let makeMargin x = FixedHeight.Create(x) :> Element
        [|
          makeMargin 60.0f

          Text.Create
            ( text = container.TextMap.descriptions.selectController
            , font = container.Font
            , zOrder = zOrders.desc
            )
          :> Element
          
          makeMargin 12.0f

          createArrow true (state.cursor > 0)

          makeMargin 12.0f

          let displaycount = 2

          buttons
            {| button = zOrders.button; buttonText = zOrders.buttonText |}
            (12.0f, false, false)
            container
            controllers.[state.cursor..min (state.cursor+displaycount-1) (controllers.Length - 1)]
            (0, current)
          :> Element

          makeMargin 12.0f

          createArrow false (state.cursor < state.selection.Length - displaycount)

        |]|> Array.map (BoxUI.alignX Align.Center))
    )

    // rightArea().With(createDesc container (container.TextMap.descriptions.selectController))
  |]