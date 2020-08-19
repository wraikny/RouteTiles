namespace RouteTiles.App

open System
open System.Threading
open System.Collections.Generic

type Updater<'model, 'msg>() =
  let mutable queue = Queue<'msg>()
  let mutable isUpdating = false

  let mutable model = ValueNone
  let mutable update' = Unchecked.defaultof<_>

  let modelEvent = Event<'model>()

  member __.Init(initModel, update) =
    update' <- update

    let mutable m = initModel
    modelEvent.Trigger(m)

    while queue.Count > 0 do
      m <- update' (queue.Dequeue()) m
      modelEvent.Trigger(m)

    model <- ValueSome m
    m

  member __.Dispatch(msg: 'msg) =
    model |> function
    | ValueSome m when not isUpdating ->
      isUpdating <- true
      let mutable m = update' msg m
      modelEvent.Trigger(m)

      while queue.Count > 0 do
        m <- update' (queue.Dequeue()) m
        modelEvent.Trigger(m)

      model <- ValueSome m
      isUpdating <- false

    | _ ->
      queue.Enqueue(msg)


  member __.Model with get() = model

  interface IObservable<'model> with
    member __.Subscribe(observer) =
      model |> ValueOption.iter(observer.OnNext >> ignore)
      modelEvent.Publish.Subscribe(observer)
