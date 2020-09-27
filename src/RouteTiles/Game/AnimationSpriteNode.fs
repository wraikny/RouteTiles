namespace RouteTiles.App

open System.Collections.Generic
open Altseed2

type internal AnimationSpriteNode(onTerminating) =
  inherit SpriteNode()

  let mutable time = 0.0f

  member val Count = Vector2I(1, 1) with get, set
  member val IsLooping = true with get, set
  member val Second = 1.0f with get, set

  member this.Terminate() =
    time <- 0.0f
    this.IsUpdated <- false
    this.IsDrawn <- false

  override this.OnUpdate() =
    if this.Texture <> null && (time < this.Second) then
      let index = int (time / this.Second * float32 (this.Count.X * this.Count.Y))

      let cdn = Vector2I(index % this.Count.X, index / this.Count.X)
      let size = this.Texture.Size / this.Count

      this.Src <- RectI(cdn * size, size).ToF()
      time <- time + Engine.DeltaSecond

      if time > this.Second then
        if this.IsLooping then
          time <- time % this.Second
        else
          this.Terminate()
          onTerminating this


[<AbstractClass>]
type internal EffectPool(initCount) =
  inherit Node()

  let mutable initCount = initCount

  let stack = Stack<AnimationSpriteNode>()
  let drawingEffects = Queue<AnimationSpriteNode>()

  let onTerminating node =
    stack.Push(node)
    drawingEffects.Dequeue() |> ignore

  let create() = new AnimationSpriteNode(onTerminating)

  abstract InitEffect: AnimationSpriteNode -> unit

  member this.Clear() =
    for node in drawingEffects do
      node.Terminate()
      stack.Push(node)

    drawingEffects.Clear()

  member this.AddEffect(f) =
    let node = stack.TryPop() |> function
      | true, node ->
        node
      | _ ->
        let node = create()
        this.InitEffect(node)
        this.AddChildNode(node)
        node

    f node
    node.IsUpdated <- true
    node.IsDrawn <- true
    drawingEffects.Enqueue(node)

  override this.OnUpdate() =
    if initCount > 0 then
      initCount <- initCount - 1

      let node = create()
      node.IsUpdated <- false
      node.IsDrawn <- false
      this.InitEffect(node)
      stack.Push(node)
      this.AddChildNode(node)

  // override this.OnRemoved() =
  //   for child in this.Children do
  //     child |> function
  //     | :? AnimationSpriteNode as n ->
  //       n.Terminate()
  //     | _ -> ()
