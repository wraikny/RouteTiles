[<AutoOpen>]
module RouteTiles.Common.Utils

open System
open System.Diagnostics

type Utils =
  [<Conditional("DEBUG")>]
  static member DebugLogn s = printfn "%s" s

  [<Obsolete>]
  static member Todo(): 'a = raise <| NotImplementedException()

  [<Obsolete>]
  static member Todo(a): 'a = printfn "TODO : %A" a; a


let inline clamp minVal maxVal x =
  if x < minVal then minVal
  elif maxVal < x then maxVal
  else x

let inline lift (x: ^a): ^b = ((^a or ^b): (static member Lift:_->_) x)

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
      let span = array.AsSpan(0, array.Length - 1)
      let res = Array.zeroCreate array.Length
      span.CopyTo(res.AsSpan(1, array.Length - 1))
      res.[0] <- item
      res, Array.last array

  let addToHead item (array: 'a[]) =
    let res = Array.zeroCreate (array.Length + 1)
    res.[0] <- item
    array.AsSpan().CopyTo(res.AsSpan(1, array.Length))
    res

  let tryPopLast array =
    if Array.isEmpty array then (ValueNone, array)
    else
      let res = Array.zeroCreate (array.Length - 1)
      array.AsSpan(1, array.Length - 1).CopyTo(res.AsSpan())
      ValueSome array.[0], array


module Array2D =
  let inline inside l1 l2 (arr: 'a[,]) =
    let (al1, al2) = Array2D.length1 arr, Array2D.length2 arr

    0 <= l1 && l1 < al1 && 0 <= l2 && l2 < al2

  let inline tryGet l1 l2 (arr: 'a[,]) =
    if inside l1 l2 arr then
      ValueSome arr.[l1,l2]
    else
      ValueNone


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

module Async =
  let CatchResult a = async {
    match! Async.Catch a with
    | Choice1Of2 x -> return Ok x
    | Choice2Of2 e -> return Error e
  }

module AsyncResult =
  let bind f (a: Async<Result<'a, 'e>>) = async {
    match! a with
    | Ok(x) -> return! f x
    | Error(e) -> return Error(e)
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