module RouteTiles.App.BoxUIElements

open System
open System.Runtime.InteropServices

open RouteTiles.App

open Altseed2
open Altseed2.BoxUI

[<AutoOpen>]
module private Ops =
  let inline (|?) a b =
    if isNull a then b else a

[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type FixedHeight private () =
  inherit Element()

  member val private Height = Unchecked.defaultof<_> with get, set

  static member Create(height: float32) =
    let elem = BoxUISystem.RentOrNull<FixedHeight>() |? FixedHeight()
    elem.Height <- height
    elem

  override this.ReturnSelf() =
    BoxUISystem.Return(this)

  override this.CalcSize(size) = new Vector2F(size.X, this.Height)

  override this.OnResize(area) =
    let area = RectF(area.Position, new Vector2F(area.Width, this.Height))

    for child in this.Children do
      child.Resize(area)

[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type ItemList private() =
  inherit Element()

  member val private ItemHeight = Unchecked.defaultof<_> with get, set
  member val private ItemMargin = Unchecked.defaultof<_> with get, set

  static member Create(?itemHeight, ?itemMargin) =
    let elem = BoxUISystem.RentOrNull<ItemList>() |? ItemList()
    elem.ItemHeight <- itemHeight
    elem.ItemMargin <- defaultArg itemMargin 0.0f
    elem

  override this.ReturnSelf() =
    BoxUISystem.Return(this)

  override this.CalcSize(size) = size

  override this.OnResize(area) =
    let itemSize = Vector2F(area.Width, defaultArg this.ItemHeight area.Width)
    let mutable pos = area.Position
    for child in this.Children do
      let size = child.GetSize(itemSize)
      child.Resize(RectF(pos, itemSize))
      pos.Y <- pos.Y + size.Y + this.ItemMargin


[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type Grid private() =
  inherit Element()

  member val private ItemSize = Unchecked.defaultof<_> with get, set

  static member Create(itemSize: float32) =
    if itemSize <= 0.0f then
      failwithf "itemSize"

    let elem = BoxUISystem.RentOrNull<Grid>() |? Grid()
    elem.ItemSize <- itemSize
    elem

  override this.ReturnSelf() =
    BoxUISystem.Return(this)

  override this.CalcSize(size) =
    let xCount = size.X / this.ItemSize |> int
    let itemMargin = size.X / (float32 xCount) / (float32 xCount - 1.0f)

    let y = (this.Children.Count - 1) / xCount

    let ySize = (float32 y) * (this.ItemSize + itemMargin) + this.ItemSize

    Vector2F(size.X, ySize)

  override this.OnResize(area) =
    let xCount = area.Width / this.ItemSize |> int

    let itemMargin = (area.Width - (float32 xCount) * this.ItemSize) / (float32 xCount - 1.0f)

    let mutable index = 0
    for child in this.Children do
      let (x, y) = (index % xCount, index / xCount)

      let pos = Vector2F(float32 x, float32 y) * (this.ItemSize + itemMargin)

      child.Resize <| RectF(area.Position + pos, Vector2F(this.ItemSize, this.ItemSize))

      index <- index + 1


open Altseed2.BoxUI.Elements

let split2 (dir: ColumnDir) (ratio: float32) item1 item2 =
  Empty.Create()
  |> BoxUI.withChildren (
    if dir = ColumnDir.X then
      [|
        Empty.Create()
        |> BoxUI.marginRight (LengthScale.Relative, 1.0f - ratio)
        |> BoxUI.withChild item1
        Empty.Create()
        |> BoxUI.marginLeft (LengthScale.Relative, ratio)
        |> BoxUI.withChild item2
      |]
    else
      [|
        Empty.Create()
        |> BoxUI.marginBottom (LengthScale.Relative, 1.0f - ratio)
        |> BoxUI.withChild item1
        Empty.Create()
        |> BoxUI.marginTop (LengthScale.Relative, ratio)
        |> BoxUI.withChild item2
      |]
  )
  :> Element
