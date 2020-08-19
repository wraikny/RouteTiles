namespace RouteTiles.App

open System.Threading
open Altseed2

type Loading(size: Vector2F, zOrder1, zOrder2) =
  inherit TransformNode()

  let mutable progSum = ValueNone
  let mutable progress = 0

  let rectBack =
    RectangleNode(
      ZOrder = zOrder1,
      RectangleSize = size,
      Color = Color(80uy, 80uy, 80uy, 255uy)
    )
  
  let rect =
    RectangleNode(
      ZOrder = zOrder2,
      Color = Color(200uy, 200uy, 200uy, 255uy)
    )
  
  do
    base.AddChildNode(rectBack)
    base.AddChildNode(rect)

  member __.Init(sum) =
    if sum > 0 then
      progSum <- ValueSome sum

  member __.Progress
    with get() = progress
    and  set(v) =
      progress <- v
      progSum |> function
      | ValueNone ->
        failwithf "progressSum is not set"
      | ValueSome progressSum ->
        rect.RectangleSize <- Vector2F(size.X * float32 v / float32 progressSum, size.Y)
