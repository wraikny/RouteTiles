namespace RouteTiles.App

open RouteTiles.Core.Utils

open System
open System.Collections.Generic
open Altseed

module Coroutine =
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

[<Sealed>]
type CoroutineNode(?capacity) =
  inherit Node()

  let capacity = defaultArg capacity 0

  let coroutines1 = List<IEnumerator<unit>>(capacity)
  let coroutines2 = List<IEnumerator<unit>>(capacity)

  let mutable oneIsSource = true
  let mutable isInsideCoroutine = false

  let getSrcDst() =
    if oneIsSource then
      (coroutines1, coroutines2)
    else
      (coroutines2, coroutines1)

  member __.Add(coroutine: seq<unit>) =
    let src, dst = getSrcDst()

    if isInsideCoroutine then
      dst.Add(coroutine.GetEnumerator())
    else
      src.Add(coroutine.GetEnumerator())

  override __.OnUpdate() =
    let src, dst = getSrcDst()

    isInsideCoroutine <- true
    for e in src do
      if e.MoveNext() then
        dst.Add(e)
    isInsideCoroutine <- false

    src.Clear()

    oneIsSource <- not oneIsSource
