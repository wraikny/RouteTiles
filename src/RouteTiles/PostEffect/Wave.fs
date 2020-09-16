namespace RouteTiles.App.PostEffect

open System.Text
open Altseed2

open RouteTiles.App

type internal Wave() =
  inherit PostEffectNode()

  let file = StaticFile.CreateStrict(Consts.PostEffect.wavepath)
  let code = file.Buffer |> Encoding.UTF8.GetString

  let shader = Shader.Create("wavecode", code, ShaderStage.Pixel)
  let material = Material.Create()
  do
    material.SetShader(shader)

  let mutable time = 0.0f

  override __.Draw(_, _) =
    time <- time + Engine.DeltaSecond
    material.SetVector4F("time", Vector4F(time, 0.0f, 0.0f, 0.0f))
    Engine.Graphics.CommandList.RenderToRenderTarget(material)
