namespace RouteTiles.App

open System.Collections.Generic
open Altseed2


type internal DrawnNodePool<'key, 'node, 'arg when 'key : equality and 'node :> Node> (setIsDrawn, create, update) =
  inherit Node()

  let objects = Dictionary<'key, 'node>()
  let pool = Stack<'node>()

  let existKeys = HashSet<'key>()

  member private this.ClearItem(key) =
    let object = objects.Item(key)
    pool.Push(object)
    object |> setIsDrawn false
    objects.Remove(key) |> ignore

  member this.Clear() =
    for x in objects do
      let item = x.Value
      pool.Push(item)
      item |> setIsDrawn false

    objects.Clear()


  member this.Update(args: #seq<'key * 'arg>) =
    for (key, arg) in args do
      objects.TryGetValue(key) |> function
      | true, object ->
        update(object, arg, false)
      | _ ->
        let object = pool.TryPop() |> function
          | true, res -> res
          | _ ->
            let n = create()
            this.AddChildNode(n)
            n

        object |> setIsDrawn true
        update(object, arg, true)
        objects.Add(key, object)

      existKeys.Add(key) |> ignore

    let removeKeys =
      [|
        for x in objects do
          if not <| existKeys.Contains(x.Key) then
            (x.Key, x.Value)
      |]

    for (key, value) in removeKeys do
      pool.Push(value)
      value |> setIsDrawn false
      objects.Remove(key) |> ignore

    existKeys.Clear()

module internal DrawnNodePool =
  let inline private setIsDrawn (isDrawn) (x: ^a) =
    (^a: (member set_IsDrawn: bool -> unit) (x, isDrawn))

  let inline init create update =
    DrawnNodePool<_,_,_>(setIsDrawn, create, update)
