module RouteTiles.App.Menu.Common

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core.Types
open RouteTiles.Core.Types.Menu
open RouteTiles.App.BoxUIElements
open RouteTiles.App

let fontName() = Font.LoadDynamicFontStrict(Consts.ViewCommon.font, 60)
let fontDesc() = Font.LoadDynamicFontStrict(Consts.ViewCommon.font, 40)

let highlighten zOrder (color: Color) twinkleEnable (movement: float32) (button: Rectangle) =
  button.AddChild(
    Rectangle.Create(zOrder = zOrder)
    |> BoxUI.onUpdate (fun (node: RectangleNode) ->
      let sinTime = MathF.Sin(Engine.Time * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)
      if movement <> 0.0f then
        button.SetMargin(LengthScale.RelativeMin, movement * sinTime) |> ignore
      
      let color =
        if twinkleEnable then
          let a = (1.0f + sinTime) * 0.5f
          let (aMin, aMax) = Consts.Menu.cursorAlphaMinMax
          let alpha = (a * (aMax - aMin) + aMin) * 255.0f |> byte
          Color (color.R, color.G, color.B, alpha)
        else
          color
      
      node.Color <- color
    )
    :> Element
  )

let highlightenSelected =
  highlighten
    ZOrder.Menu.iconSelected
    Consts.Menu.cursorColor

let highlightenCurrent =
  highlighten
    ZOrder.Menu.iconCurrent
    Consts.Menu.currentColor
    false
    0.0f


let mainMenuArea(children) =
  Empty.Create()
  |> BoxUI.withChildren [|
    Empty.Create()
    |> BoxUI.marginX (LengthScale.Relative, 0.1f)
    |> BoxUI.marginTop (LengthScale.Relative, 0.08f)
    |> BoxUI.withChildren children
  |]
  :> Element

let textButton font text =
  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.iconBackground)
  |> BoxUI.withChild (
    Text.Create(text = text, font = font, color = Consts.Menu.textColor, zOrder = ZOrder.Menu.buttonText)
    |> BoxUI.alignCenter
  )

let textButtonDesc = textButton (fontDesc())

let settingHeader (items: string[]) (current: int) =
  Column.Create(ColumnDir.X)
  |> BoxUI.withChildren [|
    for (index, name) in items |> Seq.indexed do
      let elem =
        textButtonDesc name
        |> BoxUI.margin (LengthScale.RelativeMin, 0.05f)

      
      if index = current then
        elem
        |> (
          highlighten
            ZOrder.Menu.iconCurrent
            Consts.Menu.cursorColor
            false 0.0f
        )

      yield elem
  |]
  :> Element

let verticalSelecter (height, margin) (button: string -> Rectangle) (items: string[]) (cursor: int) (current: int) =
  ItemList.Create(itemHeight = height, itemMargin = margin)
  |> BoxUI.withChildren [|
    for (index, name) in items |> Seq.indexed do
      let elem = button name

      if index = current then
        highlightenCurrent elem

      if index = cursor then
        highlightenSelected true 0.0f elem

      elem
  |]
  :> Element

type Description = { name: string; desc: string }

let sideBar (description: Description) =
  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.sideMenuBackground)
  |> BoxUI.withChild (
    ItemList.Create(itemMargin = 10.0f)
    |> BoxUI.marginXY (LengthScale.Relative, 0.05f, 0.06f)
    |> BoxUI.withChildren [|
      Text.Create(
        aspect = Aspect.Fixed,
        text = description.name,
        font = fontName(),
        zOrder = ZOrder.Menu.sideMenuText,
        color = Consts.Menu.textColor
      ) :> Element
      Text.Create(
        aspect = Aspect.Fixed,
        text = description.desc,
        font = fontDesc(),
        zOrder = ZOrder.Menu.sideMenuText,
        color = Consts.Menu.textColor
      ) :> Element
    |]
  )
  :> Element
