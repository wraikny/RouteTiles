namespace RouteTiles.App

open RouteTiles.Core.Utils

open System
open System.Collections.Generic
open Altseed2

module internal Coroutine =
  let milliseconds (ms: int<millisec>) =
    seq {
      let msf = float32 ms
      let mutable t = 0.0f
      while t < msf do
        t <- t + Engine.DeltaSecond * 1000.0f
        yield (t / msf)
    }

  let sleep ms = seq { for _ in milliseconds ms -> () }

  let loop coroutine =
    seq { while true do yield! coroutine }

  open System.Collections.Generic
  open System.Linq

  let inline toParallel (coroutines : seq<seq<unit>>) =
    seq {
      let coroutines =
        coroutines
        |> Seq.map(fun c -> c.GetEnumerator())
        |> Seq.toArray

      let mutable isContinue = true

      while isContinue do
        isContinue <- false
        for c in coroutines do
          if c.MoveNext() && not isContinue then
            isContinue <- true
        yield ()
    }

[<Sealed>]
type internal CoroutineNode(?capacity) =
  inherit Node()

  let coroutines = List<IEnumerator<unit>>(defaultArg capacity 0)
  let tmp = List<IEnumerator<unit>>()

  let mutable isInsideCoroutine = false

  member val IsUpdated = true with get, set

  member __.Add(coroutine: seq<unit>) =
    if isInsideCoroutine then
      tmp.Add(coroutine.GetEnumerator())
    else
      coroutines.Add(coroutine.GetEnumerator())

  override this.OnUpdate() =
    if this.IsUpdated then
      if tmp.Count > 0 then
        coroutines.AddRange(tmp)
        tmp.Clear()

      isInsideCoroutine <- true
      let mutable index = 0
      for i in 0..coroutines.Count-1 do
        let e = coroutines.[i]
        if e.MoveNext() then
          coroutines.[index] <- e
          index <- index + 1
      isInsideCoroutine <- false

      if coroutines.Count > index then
        coroutines.RemoveRange(index, coroutines.Count - index)
