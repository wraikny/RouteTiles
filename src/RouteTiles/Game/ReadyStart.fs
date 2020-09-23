namespace RouteTiles.App

open System.Collections.Generic
open Altseed2
open RouteTiles.Core.Utils


type ReadyStart(soundControl: SoundControl, initPosition) =
  inherit SpriteNode(IsDrawn = false, ZOrder = ZOrder.Board.readyStart)

  let readyTexture = Texture2D.LoadStrict(@"Menu/ready.png")
  let startTexture = Texture2D.LoadStrict(@"Menu/start.png")

  let mutable coroutine: IEnumerator<unit> = null

  override __.OnUpdate() =
    if coroutine <> null then
      if not <| coroutine.MoveNext() then
        coroutine <- null

  member private this.SetTex(tex) =
    this.Texture <- tex
    this.CenterPosition <- tex.Size.To2F() * 0.5f


  member this.Ready(start) =
    this.IsDrawn <- true
    this.Position <- initPosition
    this.SetTex(readyTexture)
    this.Color <- Color(255, 255, 255, 0)

    soundControl.PlaySE(SEKind.ReadyGame, true)

    coroutine <- (seq {
      for t in Coroutine.milliseconds 1500<millisec> do
        this.Color <- Color(255, 255, 255, 255.0f * t |> int)
        this.Position <- initPosition + Vector2F(0.0f, Easing.GetEasing(EasingType.InQuad, t) * 20.0f)
        yield ()

      this.Color <- Color(255, 255, 255, 255)
      this.Position <- initPosition + Vector2F(0.0f, 20.0f)

      this.SetTex(startTexture)
      soundControl.PlaySE(SEKind.StartGame, true)
      start()

      let flushInterval = 200<millisec>

      yield! Coroutine.toParallel [|
        seq {
          for t in Coroutine.milliseconds (6 * flushInterval) do
            this.Color <- Color(255, 255, 255, 255.0f * (1.0f - t) |> int)
            this.Position <- initPosition + Vector2F(0.0f, 20.0f - Easing.GetEasing(EasingType.InQuad, t) * 20.0f)
            yield ()
          }
        seq {
          for _ in 1..3 do
            this.IsDrawn <- true
            
            yield! Coroutine.sleep flushInterval
            this.IsDrawn <- false

            yield! Coroutine.sleep flushInterval
        }
      |]

      this.Position <- initPosition
      this.IsDrawn <- false
    }).GetEnumerator()