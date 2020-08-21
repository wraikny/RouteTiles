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

let highlighten (movement: float32) (color: Color) (button: Rectangle) =
  let mutable selectedTime = 0.0f
  button.AddChild(
    Rectangle.Create(zOrder = ZOrder.Menu.iconSelected)
    |> BoxUI.onUpdate (fun (node: RectangleNode) ->
      let sinTime = MathF.Sin(selectedTime * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)
      if movement <> 0.0f then
        button.SetMargin(LengthScale.RelativeMin, movement * sinTime) |> ignore
      
      let a = (1.0f + sinTime) * 0.5f
      let (aMin, aMax) = Consts.Menu.cursorAlphaMinMax
      let alpha = (a * (aMax - aMin) + aMin) * 255.0f |> byte
      let color = Color (color.R, color.G, color.B, alpha)
      node.Color <- color
      selectedTime <- selectedTime + Engine.DeltaSecond)
    :> Element
  )

let mainMenuArea(children) =
  Empty.Create()
  |> BoxUI.withChildren [|
    Empty.Create()
    |> BoxUI.marginX (LengthScale.Relative, 0.1f)
    |> BoxUI.marginTop (LengthScale.Relative, 0.08f)
    |> BoxUI.withChildren children
  |]
  :> Element

let textButton text =
  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.iconBackground)
  |> BoxUI.withChild (
    Text.Create(text = text, font = fontDesc(), color = Consts.Menu.textColor, zOrder = ZOrder.Menu.icon)
    |> BoxUI.alignCenter
  )

let settingHeader (items: string[]) (current: int) =
  Column.Create(ColumnDir.X)
  |> BoxUI.withChildren [|
    for (index, name) in items |> Seq.indexed do
      let elem =
        textButton name
        |> BoxUI.margin (LengthScale.RelativeMin, 0.05f)

      if index = current then
        highlighten 0.0f (Consts.Menu.cursorColor) elem

      elem
  |]
  :> Element

let verticalSelecter (items: string[]) (cursor: int) (current: int) =
  ItemList.Create(itemHeight = 40.0f, itemMargin = 10.0f)
  |> BoxUI.withChildren [|
    for (index, name) in items |> Seq.indexed do
      let elem = textButton name

      if index = current then
        highlighten 0.0f (Color(50uy, 50uy, 200uy)) elem

      if index = cursor then
        highlighten -0.05f (Consts.Menu.cursorColor) elem

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
