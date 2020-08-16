[<AutoOpen>]
module RouteTiles.Core.Utils

let inline clamp minVal maxVal x =
  if x < minVal then minVal
  elif maxVal < x then maxVal
  else x

let inline lift (x: ^a): ^b = ((^a or ^b): (static member Lift:_->_) x)

module Option =
  let inline alt (f: unit -> 'a option) (o: 'a option) =
    o |> function
    | None -> f()
    | _ -> o

module ValueOption =
  let inline alt (f: unit -> 'a voption) (o: 'a voption) =
    o |> function
    | ValueNone -> f()
    | _ -> o

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

[<Struct; CustomEquality; NoComparison>]
type SetOf2<'a when 'a : equality> = private | SetOf2 of 'a * 'a

with
  override x.Equals(yobj) = 
    match yobj with
    | :? SetOf2<'a> as y ->
      match x, y with
      | SetOf2(xa, xb), SetOf2(ya, yb) when (xa = ya && xb = yb) || (xa = yb && xb = ya) -> true
      | _ -> false
    | _ -> false

    override x.GetHashCode() =
      x |> function
      | SetOf2(a, b) ->
        hash a ||| hash b

    interface System.IEquatable<SetOf2<'a>> with
      member this.Equals(that : SetOf2<'a>) =
          this.Equals(that)