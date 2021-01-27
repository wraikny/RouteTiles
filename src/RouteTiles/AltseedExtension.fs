[<AutoOpen>]
module RouteTiles.App.Altseed2Extension

open Altseed2

module Shader =
  let tryCreateFromFile name path stage =
    let mutable shader = null
    let msg = Shader.TryCreateFromFile(name, path, stage, &shader)
    if isNull shader then Error msg else Ok shader


let mutable time = 0.0f

type Engine with
  static member InitializeEx(title, size: Vector2I, config) =
    time <- 0.0f
    if not <| Engine.Initialize(title, size.X, size.Y, config) then
      failwith "Failed to initialize the Altseed2"

  static member Run() =
#if DEBUG
    let rec dumpNodes t (node: Node) =
      printfn "%s%s" t (node.GetType().Name)
      for child in node.Children do
        dumpNodes (t + "  ") child
#endif

    let rec loop() =
      if Engine.DoEvents() then
        time <- time + Engine.DeltaSecond
        BoxUI.BoxUISystem.Update()
        Engine.Update() |> ignore
        loop()

    loop()
  
  static member TerminateEx() =
    BoxUI.BoxUISystem.Terminate()
    Engine.Terminate()

  static member Time with get() = time
