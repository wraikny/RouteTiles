module internal RouteTiles.App.Menu.ElementCommon

open System
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Common
open RouteTiles.Menu
open RouteTiles.Menu.SubMenu
open RouteTiles.Menu.Types
open RouteTiles.Menu.Effects
open RouteTiles.App
open RouteTiles.App.BoxUIElements

let replaceOne (s: string) = s.Replace("1", " 1")

let secondToDisplayTime second =
  sprintf "%02i:%02i.%02i"
    (second / 60.0f |> int)
    (second % 60.0f |> int)
    (int(second * 100.0f) % 100)

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

let createHighlightUpdate (baseElem: Element) (textElem: Text) =
  let startTime = Engine.Time
  (fun (node: SpriteNode) ->
    let dTime = Engine.Time - startTime
    
    let mutable finished = false
    if baseElem <> null && not finished then
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
            |> BoxUI.onUpdate (createHighlightUpdate (if enabledCursorAnimation then baseElem else null) textElem)
            :> Element
          )

      |]
      )
  )

let centeredSprite zOrder texture =
  Sprite.Create
    ( aspect = Aspect.Fixed
    , texture = texture
    , zOrder = zOrder
    )
  |> BoxUI.alignCenter
  :> Element

let modalFrame zOrder (container: Container) =
  centeredSprite zOrder container.InputBackground

let moreArrowSize = Vector2F(24.0f, 24.0f)

let createArrow (container: Container) zOrder (isUp: bool) (drawn: bool) =
  let elem = FixedSize.Create(moreArrowSize) :> Element

  if drawn then
    elem.AddChild(
      Sprite.Create
        ( aspect = Aspect.Fixed
        , texture = container.SelectionArrow
        // , verticalFlip = flip
        , src = Nullable (RectF((if isUp then moreArrowSize.X else 0.0f), 0.0f, moreArrowSize.X, moreArrowSize.Y))
        , zOrder = zOrder
        )
      |> BoxUI.alignCenter
    )

  elem
  |> BoxUI.debug

let makeMargin x = FixedHeight.Create(x) :> Element

let listSelectorModal
  (zOrders: {| button: int; buttonText: int; desc: int; background: int |})
  (container: Container)
  (title: string)
  (font: Font)
  (state: ListSelector.State<'a>)
  (selections: string[])
  =
  let current = state.current |> Option.filter((=) state.cursor) |> Option.map(fun _ -> state.cursor)

  let createArrow = createArrow container zOrders.button

  [|
    modalFrame zOrders.background container
    |> BoxUI.withChild (
      ItemList.Create()
      |> BoxUI.withChildren (
        [|
          makeMargin 60.0f

          Text.Create
            ( text = title
            , font = font
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
            selections.[state.cursor..min (state.cursor+displaycount-1) (selections.Length - 1)]
            (0, current)
          :> Element

          makeMargin 12.0f

          createArrow false (state.cursor < state.selection.Length - displaycount)

        |]|> Array.map (BoxUI.alignX Align.Center))
    )

    // rightArea().With(createDesc container (container.TextMap.descriptions.selectController))
  |]


let controllerSelect (zOrders: {| button: int; buttonText: int; desc: int; background: int |}) (container: Container) (state: ListSelector.State<Controller>) =
  let selections = state.selection |> Array.map(function
    | Controller.KeyboardShift -> container.TextMap.buttons.keyboardShift
    | Controller.KeyboardSeparate -> container.TextMap.buttons.keyboardSeparate
    | Controller.Joystick(_, name, _) -> name)

  listSelectorModal
    zOrders
    container
    container.TextMap.descriptions.selectController
    container.DynamicFont
    state
    selections


let gameInfoFrameSize = Vector2F(480.0f, 68.0f)

let twoSplitFrameWithSrc src zOrder (container: Container) =
  Sprite.Create
    ( aspect = Aspect.Fixed
      , texture = container.GameInfoFrame
      , zOrder = zOrder
      , src = Nullable(src)
    )

let twoSplitFrame: int -> Container -> _ = twoSplitFrameWithSrc <| RectF(Vector2F(0.0f, 0.0f), gameInfoFrameSize)
let twoSplitFrameHighlight: int -> Container -> _ = twoSplitFrameWithSrc <| RectF(Vector2F(0.0f, gameInfoFrameSize.Y), gameInfoFrameSize)

let twoSplitFrameXMargin = 32.0f
