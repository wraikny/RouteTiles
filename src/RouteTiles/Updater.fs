namespace RouteTiles.App

open System
open System.Threading
open System.Collections.Generic

[<Sealed>]
type internal Updater<'model, 'msg>() =
  let mutable queue = Queue<'msg>()
  let mutable isUpdating = false

  let mutable model: 'model voption = ValueNone
  let mutable update = Unchecked.defaultof<_>

  let modelEvent = Event<'model>()

  let applyMsgs inModel =
    let rec f model' =
      queue.TryDequeue()|> function
      | true, msg ->
        let m = update msg model'
        modelEvent.Trigger m
        f m
      | _ -> model'

    isUpdating <- true
    model <- ValueSome <| f inModel
    isUpdating <- false

  member __.Init(initModel, update') =
    update <- update'
    modelEvent.Trigger(initModel)
    applyMsgs initModel


  member __.Dispatch(msg: 'msg) =
    queue.Enqueue(msg)

    model |> function
    | ValueSome m when not isUpdating ->
      applyMsgs m
    | _ -> ()

  member __.Model with get() = model

  interface IObservable<'model> with
    member __.Subscribe(observer) =
      // model |> ValueOption.iter(observer.OnNext >> ignore)
      modelEvent.Publish.Subscribe(observer)
