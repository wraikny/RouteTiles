module RouteTiles.App.ErrorLog

open RouteTiles.Core

open System
open System.Threading
open System.IO
open System.Collections.Concurrent
// open System.Runtime.Serialization
// open System.Runtime.Serialization.Formatters.Binary

let [<Literal>] ErrorLogFile = @"RouteTiles.Error.log"

let dirName = Path.GetDirectoryName ErrorLogFile

let private errorToMessage date (error: exn) =
    sprintf """%s

%s

%s

%s

----------------------------------------
"""
      date
      (error.GetType().Name)
      error.Message
      error.StackTrace

let private write (error: exn) =
  let now = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")
  let message =
    error |> function
    | :? AggregateException as e ->
      e.Flatten().InnerExceptions
      |> Seq.map (errorToMessage now)
      |> String.concat "\n"
    | _ -> errorToMessage now error

  File.AppendAllTextAsync(ErrorLogFile, message)


let private writeQueue = ConcurrentQueue<exn>()

let save = writeQueue.Enqueue

let private lockObj = obj()
let mutable private config = ValueNone
let tryGet() = lock lockObj (fun () -> config)

let update = async {
  // let ctx = SynchronizationContext.Current
  do! Async.SwitchToThreadPool()
  while true do
    match writeQueue.TryDequeue() with
    | true, conf ->
      do! write conf |> Async.AwaitTask
      lock lockObj (fun () -> config <- ValueSome conf)
      // do! Async.SwitchToContext ctx
    | _ -> do! Async.Sleep 100
}