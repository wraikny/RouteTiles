[<AutoOpen>]
module RouteTiles.Core.Utils

open RouteTiles.Core.Effects

let random = Random.RandomBuilder()

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

[<Measure>] type sec
[<Measure>] type millisec
