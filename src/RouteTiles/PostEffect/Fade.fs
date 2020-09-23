namespace RouteTiles.App.PostEffect

open Altseed2
open RouteTiles.App

type PostEffectFade() as this =
  inherit PostEffectNode()

  let material = Material.Create()
  do
    Shader.tryCreateFromFile "Fade" Consts.PostEffect.fadepath ShaderStage.Pixel
    |> function
    | Ok shader -> material.SetShader(shader)
    | Error e -> failwith e

  let mutable fadeRate = 1.0f
  let mutable fadeColor = Vector3F(1.0f, 0.0f, 0.0f)

  do
    this.FadeRate <- 0.0f
    this.FadeColor <- Vector3F(0.0f, 0.0f, 0.0f)

  member __.FadeRate
    with get() = fadeRate
    and set(v) =
      if fadeRate <> v then
        fadeRate <- v
        material.SetVector4F("fadeRate", Vector4F(v, 0.0f, 0.0f, 0.0f))

  member __.FadeColor
    with get() = fadeColor
    and set(v) =
      if v <> fadeColor then
        fadeColor <- v
        material.SetVector4F("fadeColor", Vector4F(v.X, v.Y, v.Z, 0.0f))

  override __.Draw(src, _) =
    material.SetTexture("mainTex", src)
    Engine.Graphics.CommandList.RenderToRenderTarget(material)
