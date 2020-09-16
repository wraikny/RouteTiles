namespace RouteTiles.App

open Altseed2

type internal AnimationSpriteNode(pushToPool) =
  inherit SpriteNode()

  let mutable time = 0.0f

  member val IsUpdated = true with get, set
  member val Count = Vector2I(1, 1) with get, set
  member val IsLooping = true with get, set
  member val Second = 1.0f with get, set

  member this.Terminate() =
    time <- 0.0f
    this.IsUpdated <- false
    this.IsDrawn <- false
    pushToPool this

  override this.OnUpdate() =
    if this.IsUpdated && this.Texture <> null && (time < this.Second) then
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

open System.Collections.Generic

[<AbstractClass>]
type internal EffectPool(initCount) =
  inherit Node()

  let mutable initCount = initCount

  let stack = Stack<AnimationSpriteNode>()

  let create() = new AnimationSpriteNode(stack.Push)

  abstract InitEffect: AnimationSpriteNode -> unit

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

  override this.OnUpdate() =
    if initCount > 0 then
      initCount <- initCount - 1

      let node = create()
      node.IsUpdated <- false
      node.IsDrawn <- false
      this.InitEffect(node)
      stack.Push(node)
      this.AddChildNode(node)

  override this.OnRemoved() =
    for child in this.Children do
      child |> function
      | :? AnimationSpriteNode as n ->
        n.Terminate()
      | _ -> ()
