[<AutoOpen>]
module RouteTiles.Core.Utils

open RouteTiles.Core.Effects

let random = Random.RandomBuilder()

let inline clamp minVal maxVal x =
  if x < minVal then minVal
  elif maxVal < x then maxVal
  else x

module Array =
  let inline pushFrontPopBack (item: 'a) (array: 'a []): 'a[] * 'a =
    if Array.isEmpty array then Array.empty, item
    else
      [|
        yield item
        yield! array.[0..array.Length-2]
      |], Array.last array

  let inline mapOfIndex (index: int) (f: 'a -> 'a) (array: 'a[]): 'a[] =
    [|
      yield! array.[0..index-1]
      yield f array.[index]
      yield! array.[index+1..array.Length-1]
    |]

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

[<Measure>] type sec
[<Measure>] type millisec

