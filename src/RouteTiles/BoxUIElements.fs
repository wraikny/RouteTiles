namespace RouteTiles.App.BoxUIElements

open RouteTiles.App

open Altseed2
open Altseed2.BoxUI

[<AutoOpen>]
module private Ops =
  let inline (|?) a b =
    if isNull a then b else a

[<AllowNullLiteral>]
type Empty private () =
  inherit Element()

  static member Create() =
    BoxUISystem.RentOrNull<Empty>() |? Empty()

  override this.ReturnSelf() =
    BoxUISystem.Return(this)

  override __.CalcSize(size) = size

  override this.OnResize(area) =
    for child in this.Children do child.Resize(area)


[<AllowNullLiteral>]
type Grid private() =
  inherit Element()

  member val ItemSize = Unchecked.defaultof<_> with get, set

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
