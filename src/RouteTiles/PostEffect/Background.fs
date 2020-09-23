namespace RouteTiles.App.PostEffect

open System.Text
open Altseed2

open RouteTiles.App

[<AllowNullLiteral>]
type internal PostEffectBackground() =
  inherit PostEffectNode()

  let material = Material.Create()

  let mutable time = 0.0f

  do
    let ws = Engine.WindowSize.To2F()
    material.SetVector4F("windowSize", Vector4F(ws.X, ws.Y, 0.0f, 0.0f))

  let setTime x =
    time <- x
    material.SetVector4F("time", Vector4F(x, 0.0f, 0.0f, 0.0f))

  member __.SetShader(path) =
    Shader.tryCreateFromFile "Background" path ShaderStage.Pixel
    |> function
    | Ok shader -> material.SetShader(shader)
    | Error e -> failwith e
    

    setTime 0.0f

  override __.OnUpdate() =
    setTime <| time + Engine.DeltaSecond

  override __.Draw(_, _) =
    Engine.Graphics.CommandList.RenderToRenderTarget(material)

open Altseed2.BoxUI
open RouteTiles.App.BoxUIElements
open RouteTiles.App.BoxUIElements.ElementOps


[<AllowNullLiteral; Sealed; AutoSerializable(true)>]
type internal BackgroundElement private () =
  inherit PostEffectElement<PostEffectBackground>()

  member val private path: string = null with get, set

  static member Create(path: string, ?zOrder: int, ?cameraGroup: uint64) =
    if isNull path then invalidArg "path" "path should not be null"

    let elem = BoxUISystem.RentOrNull<BackgroundElement>() |? (fun () -> BackgroundElement())
    elem.path <- path
    elem.zOrder <- defaultArg zOrder 0
    elem.cameraGroup <- defaultArg cameraGroup 0uL
    elem

  override this.ReturnSelf() =
    base.ReturnSelf()
    BoxUISystem.Return<BackgroundElement>(this)

  override this.OnAdded() =
    base.OnAdded()
    this.Node.SetShader this.path

