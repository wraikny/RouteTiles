module internal RouteTiles.App.BoxUIElements

open System
open System.Runtime.InteropServices

open RouteTiles.App

open Altseed2
open Altseed2.BoxUI

module internal ElementOps =
  let inline (|?) a f =
    if isNull a then f() else a

open ElementOps

[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type FixedHeight private () =
  inherit Element()

  member val private Height = Unchecked.defaultof<_> with get, set

  static member Create(height: float32) =
    let elem = BoxUISystem.RentOrNull<FixedHeight>() |? (fun () -> FixedHeight())
    elem.Height <- height
    elem

  override this.ReturnSelf() =
    BoxUISystem.Return(this)

  override this.CalcSize(size) = new Vector2F(size.X, this.Height)

  override this.OnResize(area) =
    let area = this.LayoutArea(area)
    let area = RectF(area.Position, new Vector2F(area.Width, this.Height))

    for child in this.Children do
      child.Resize(area)

[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type FixedWidth private () =
  inherit Element()

  member val private Width = Unchecked.defaultof<_> with get, set

  static member Create(width: float32) =
    let elem = BoxUISystem.RentOrNull<FixedWidth>() |? (fun () -> FixedWidth())
    elem.Width <- width
    elem

  override this.ReturnSelf() =
    BoxUISystem.Return(this)

  override this.CalcSize(size) = new Vector2F(this.Width, size.Y)

  override this.OnResize(area) =
    let area = this.LayoutArea(area)
    let area = RectF(area.Position, new Vector2F(this.Width, area.Height))

    for child in this.Children do
      child.Resize(area)

[<AllowNullLiteral; AbstractClass>]
type PostEffectElement<'p when 'p :> PostEffectNode and 'p : null and 'p : (new : unit -> 'p)> () =
  inherit Element()

  let mutable node: 'p = null
  member val zOrder = Unchecked.defaultof<int> with get, set
  member val cameraGroup = Unchecked.defaultof<uint64> with get, set
  member __.Node with get() = node and set(v) = node <- v

  override this.ReturnSelf() =
    this.Root.Return(node)
    node <- null

  override this.OnAdded() =
    node <- this.Root.RentOrCreate<'p>()
    node.ZOrder <- this.zOrder
    node.CameraGroup <- this.cameraGroup

  override this.CalcSize(size) = size

  override this.OnResize(area) =
    let area = this.LayoutArea(area)
    for child in this.Children do
      child.Resize(area)


[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type GaussianBlur private () =
  inherit PostEffectElement<PostEffectGaussianBlurNode>()

  member val private intensity = Unchecked.defaultof<float32> with get, set

  static member Create(?intensity: float32, ?zOrder: int, ?cameraGroup: uint64) =
    let elem = BoxUISystem.RentOrNull<GaussianBlur>() |? (fun () -> GaussianBlur())
    elem.intensity <- defaultArg intensity 5.0f
    elem.zOrder <- defaultArg zOrder 0
    elem.cameraGroup <- defaultArg cameraGroup 0uL
    elem

  override this.ReturnSelf() =
    base.ReturnSelf()
    BoxUISystem.Return(this)

  override this.OnAdded() =
    base.OnAdded()
    this.Node.Intensity <- this.intensity



[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type ItemList private() =
  inherit Element()

  member val private ItemHeight = Unchecked.defaultof<_> with get, set
  member val private ItemMargin = Unchecked.defaultof<_> with get, set

  static member Create(?itemHeight, ?itemMargin) =
    let elem = BoxUISystem.RentOrNull<ItemList>() |? (fun () -> ItemList())
    elem.ItemHeight <- itemHeight
    elem.ItemMargin <- defaultArg itemMargin 0.0f
    elem

  override this.ReturnSelf() =
    BoxUISystem.Return(this)

  override this.CalcSize(size) =
    let folder = this.ItemHeight |> function
      | None ->
        let itemSize = Vector2F(size.X, size.X)

        (fun (x, y) (child: Element) ->
          let csize = child.GetSize(itemSize)
          (max x csize.X, y + csize.Y)
        )

      | Some itemHeight ->
        let itemSize = Vector2F(size.X, itemHeight)

        (fun (x, y) child ->
          let csize = child.GetSize(itemSize)
          (max x csize.X, y + itemHeight)
        )

    this.Children
    |> Seq.fold folder (0.0f, 0.0f)
    |> fun (x, y) -> (x, y + this.ItemMargin * (float32 this.Children.Count - 1.0f))
    |> Vector2F

  override this.OnResize(area) =
    let area = this.LayoutArea(area)
    this.ItemHeight |> function
    | None ->
      let itemSize = Vector2F(area.Width, area.Width)
      let mutable pos = area.Position
      for child in this.Children do
        let size = child.GetSize(itemSize)
        child.Resize(RectF(pos, itemSize))
        pos.Y <- pos.Y + size.Y + this.ItemMargin
    | Some itemHeight ->
      let itemSize = Vector2F(area.Width, itemHeight)
      let mutable pos = area.Position
      for child in this.Children do
        child.Resize(RectF(pos, itemSize))
        pos.Y <- pos.Y + itemHeight + this.ItemMargin


[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type Grid private() =
  inherit Element()

  member val private ItemSize = Unchecked.defaultof<_> with get, set

  static member Create(itemSize: Vector2F) =
    if itemSize.X <= 0.0f || itemSize.Y <= 0.0f then
      failwithf "invalid itemSize %O" itemSize

    let elem = BoxUISystem.RentOrNull<Grid>() |? (fun () -> Grid())
    elem.ItemSize <- itemSize
    elem

  override this.ReturnSelf() =
    BoxUISystem.Return(this)

  override this.CalcSize(size) =
    let xCount = size.X / this.ItemSize.X |> int
    let itemMargin = size.X / (float32 xCount) / (float32 xCount - 1.0f)

    let y = (this.Children.Count - 1) / xCount

    let ySize = (float32 y) * (this.ItemSize.Y + itemMargin) + this.ItemSize.Y

    Vector2F(size.X, ySize)

  override this.OnResize(area) =
    let area = this.LayoutArea(area)
    let xCount = area.Width / this.ItemSize.X |> int

    let itemMargin = (area.Width * Vector2F(1.0f, 1.0f) - (float32 xCount) * this.ItemSize) / (float32 xCount - 1.0f)

    let mutable index = 0
    for child in this.Children do
      let (x, y) = (index % xCount, index / xCount)

      let pos = Vector2F(float32 x, float32 y) * (this.ItemSize + itemMargin)

      child.Resize <| RectF(area.Position + pos, this.ItemSize)

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
