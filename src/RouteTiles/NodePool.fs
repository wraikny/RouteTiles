namespace RouteTiles.App

open System.Collections.Generic
open Altseed

[<AbstractClass>]
type NodePool<'key, 'node, 'arg when 'key : equality and 'node :> Node>() =
  inherit Node()

  let objects = Dictionary<'key, 'node>()
  let pool = Stack<'node>()

  let existKeys = HashSet<'key>()

  abstract Create: unit -> 'node
  abstract Update: 'node * 'arg -> unit

  member this.Update(args) =
    for (key, arg) in args do
      objects.TryGetValue(key) |> function
      | true, object ->
        this.Update(object, arg)
      | _ ->
        let object = pool.TryPop() |> function
          | true, res -> res
          | _ -> this.Create()

        this.Update(object, arg)
        objects.Add(key, object)
        this.AddChildNode(object)

      existKeys.Add(key) |> ignore

    [|
      for x in objects do
        if not <| existKeys.Contains(x.Key) then
          yield x.Key
    |]
    |> Array.iter(fun key ->
      let object = objects.Item(key)
      pool.Push(object)
      this.RemoveChildNode(object)
    )
