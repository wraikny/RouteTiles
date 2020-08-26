[<AutoOpen>]
module RouteTiles.Core.Utils

open System
open System.Diagnostics

type Utils =
  [<Conditional("DEBUG")>]
  static member DebugLogfn format = Printf.kprintf (printfn "%s") format

  [<Obsolete; Conditional("DEBUG")>]
  static member Todo(): 'a = raise <| NotImplementedException()


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
  open System

  let inline pushFrontPopBack (item: 'a) (array: 'a []): 'a[] * 'a =
    if Array.isEmpty array then Array.empty, item
    else
      let span = array.AsSpan().Slice(0, array.Length - 1)
      let res = Array.zeroCreate array.Length
      span.CopyTo(res.AsSpan().Slice(1, array.Length - 1))
      res.[0] <- item
      res, Array.last array


module Array2D =
  let inline tryGet x y (arr: 'a[,]) =
    let (w, h) = Array2D.length1 arr, Array2D.length2 arr

    if 0 <= x && x < w && 0 <= y && y < h
    then ValueSome arr.[x,y]
    else ValueNone


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

open System

[<Struct; CustomEquality; NoComparison>]
type SetOf2<'a when 'a : equality> = SetOf2 of 'a * 'a

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
        HashCode.Combine(a, b)

    interface System.IEquatable<SetOf2<'a>> with
      member this.Equals(that : SetOf2<'a>) =
          this.Equals(that)