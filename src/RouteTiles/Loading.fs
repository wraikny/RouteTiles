namespace RouteTiles.App

open System.Threading
open Altseed2

type Loading(progressSum, size: Vector2F, zOrder1, zOrder2) =
  inherit TransformNode()

  let ctx = SynchronizationContext.Current
  let mutable progress = 0

  let rectBack =
    RectangleNode(
      ZOrder = zOrder1,
      RectangleSize = size,
      Color = Consts.Menu.elementBackground.Value
    )
  
  let rect =
    RectangleNode(
      ZOrder = zOrder2,
      Color = Color(200uy, 200uy, 200uy, 255uy)
    )
  
  do
    base.AddChildNode(rectBack)
    base.AddChildNode(rect)

  let setProgress(p) =
    rect.RectangleSize <- Vector2F(size.X * float32 p / float32 progressSum, size.Y)

  member __.Progress() =
    let p = Interlocked.Increment (&progress)

    if SynchronizationContext.Current <> ctx then
      ctx.Post((fun _ -> setProgress(p)), ())
    else
      setProgress(p)

    p
