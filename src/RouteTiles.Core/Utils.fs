module RouteTiles.Core.Utils

module Array =
  let inline pushFrontPopBack (item: 'a) (array: 'a []): 'a[] * 'a option =
    [|
      yield item
      yield! array.[0..array.Length-2]
    |], Array.tryLast array

  let inline mapOfIndex (index: int) (f: 'a -> 'a) (array: 'a[]): 'a[] =
    [|
      for i in 0..index-1 -> array.[i]
      yield f array.[index]
      for i in index+1..array.Length-1 -> array.[i]
    |]
