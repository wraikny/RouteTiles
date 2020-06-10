namespace RouteTiles.App

open Altseed

type AnimationSpriteNode() =
  inherit SpriteNode()

  let mutable time = 0.0f

  member __.Reset() = time <- 0.0f

  member val Count = Vector2I(1, 1) with get, set
  member val IsLooping = true with get, set
  member val Second = 1.0f with get, set

  abstract OnAnimationFinished: unit -> unit
  default x.OnAnimationFinished() = ()

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

        this.OnAnimationFinished()

