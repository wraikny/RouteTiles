module RouteTiles.App.Program

open System.Threading

open Altseed2
open RouteTiles.Core
open RouteTiles.Core.Types

open RouteTiles.App.Consts.ViewCommon

[<EntryPoint; System.STAThread>]
let main _ =
  let inline init(config) =
    if not <| Engine.Initialize("RouteTiles", windowSize.X, windowSize.Y, config) then
      failwith "Failed to initialize the Altseed2"

    Engine.ClearColor <- clearColor

  let initializers = [|
    Consts.initialize
    MenuElement.initialize
  |]

  let initResources() =
    Engine.AddNode(PostEffect.Wave(ZOrder = ZOrder.posteffect))

    let ctx = SynchronizationContext.Current

    let loadingSize = windowSize.To2F() * Vector2F(0.75f, 0.125f)
    let loading = Loading(loadingSize, 0, 1)
    loading.Position <- (Engine.WindowSize.To2F() - loadingSize) * 0.5f


    let progressSum, initializers =
      let mutable count = 0
      let progress () =
        let c = Interlocked.Increment (&count)
        ctx.Post((fun _ ->
          loading.Progress <- c
        ), ())
        c

      initializers
      |> Array.map(fun i -> i progress)
      |> Array.unzip
      |> fun (ps, is) ->
        Array.sum ps,
        Async.Parallel is |> Async.Ignore

    loading.Init(progressSum)

    Engine.AddNode(loading)

    async {
      let ctx = SynchronizationContext.Current

      do! initializers

      do! Async.SwitchToContext ctx

      let node = Menu()
      Engine.RemoveNode(loading)
      Engine.AddNode(node)
    }
    |> Async.StartImmediate

  let rec loop() =
    if Engine.DoEvents() then
      // printfn "%f" Engine.DeltaSecond
      BoxUI.BoxUISystem.Update()
      Engine.Update() |> ignore
      loop()

  let config =
    Configuration(
      FileLoggingEnabled = true,
      LogFileName = "Log.txt"
    )

#if DEBUG
  config.ConsoleLoggingEnabled <- true

  init(config)

  if not <| Engine.File.AddRootDirectory(@"Resources") then
    failwithf "Failed to add root directory"

  initResources()
  loop()

  BoxUI.BoxUISystem.Terminate()
  Engine.Terminate()
#else

  try
    init(config)

    try
      if not <| Engine.File.AddRootPackageWithPassword(@"Resources.pack", ResourcesPassword.password) then
        failwithf "Failed to add root package"

      initResources()
      loop()
    with e ->
      Engine.Log.Error(LogCategory.User, sprintf "%A: %s" (e.GetType()) e.Message)
      reraise()

    BoxUI.BoxUISystem.Terminate()
    Engine.Terminate()
  with e ->
    printfn "%A: %s" (e.GetType()) e.Message
#endif

  0 // return an integer exit code
