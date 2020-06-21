[<AutoOpen>]
module RouteTiles.Core.Utils

open RouteTiles.Core.Effects

let random = Random.RandomBuilder()

let inline clamp minVal maxVal x =
  if x < minVal then minVal
  elif maxVal < x then maxVal
  else x

let inline lift (x: ^a): ^b = ((^a or ^b): (static member Lift:_->_) x)

module Array =
  let inline pushFrontPopBack (item: 'a) (array: 'a []): 'a[] * 'a =
    if Array.isEmpty array then Array.empty, item
    else
      [|
        yield item
        yield! array.[0..array.Length-2]
      |], Array.last array

  // let inline mapOfIndex (index: int) (f: 'a -> 'a) (array: 'a[]): 'a[] =
  //   [|
  //     yield! array.[0..index-1]
  //     yield f array.[index]
  //     yield! array.[index+1..array.Length-1]
  //   |]

module Array2D =
  let inline tryGet x y (arr: 'a[,]) =
    let (w, h) = Array2D.length1 arr, Array2D.length2 arr

    if 0 <= x && x < w && 0 <= y && y < h
    then ValueSome arr.[x,y]
    else ValueNone

  let inline toSeq (arr: 'a[,]) =
    let (w, h) = Array2D.length1 arr, Array2D.length2 arr
    seq {
      for x in 0..w-1 do
      for y in 0..h-1 do
        yield arr.[x, y]
    }

module Seq =
  let inline filterMap f (xs: seq<'a>): seq<'b> =
    seq {
      for x in xs do
        match f x with
        | Some r -> yield r
        | None -> ()
    }

  let inline filterMapV f (xs: seq<'a>): seq<'b> =
    seq {
      for x in xs do
        match f x with
        | ValueSome r -> yield r
        | ValueNone -> ()
    }

[<Measure>] type sec
[<Measure>] type millisec

