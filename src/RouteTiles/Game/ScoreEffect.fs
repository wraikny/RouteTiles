namespace RouteTiles.App

open RouteTiles.Core.Utils

open System.Collections.Generic
open Altseed2

type ScoreEffect(font, color, addCoroutine: seq<unit> -> unit) =
  inherit Node()

  let stack = Stack<TextNode>()
  let drawingQueue = Queue<TextNode>()

  member __.Clear() =
    for o in drawingQueue do
      o.IsDrawn <- false
      stack.Push(o)

    drawingQueue.Clear()

  member this.Add(score: int, position: Vector2F) =
    Utils.DebugLogn(sprintf "ScoreEffect.Add(%d, %A)" score position)
    let o = stack.TryPop() |> function
      | true, node -> node
      | _ ->
        let node = TextNode(Font = font, ZOrder = ZOrder.Board.scoreEffect)
        this.AddChildNode(node)
        node

    o.IsDrawn <- true
    o.Color <- color
    o.Position <- position
    o.Text <- sprintf "+%dpt." score
    o.CenterPosition <- o.ContentSize * 0.5f
    drawingQueue.Enqueue(o)

    addCoroutine(seq {
      for t in Coroutine.milliseconds 1000<millisec> do
        o.Position <- position + Vector2F(0.0f, -20.0f * t * t)
        o.Color <- Color(color.R, color.G, color.B, 255.0f * (1.0f - t * t) |> byte)
        yield ()

      o.IsDrawn <- false
      stack.Push(o)
      drawingQueue.Dequeue() |> ignore
    })
