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

let toString (error: exn) =
  let now = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")

  error |> function
  | :? AggregateException as e ->
    e.Flatten().InnerExceptions
    |> Seq.map (errorToMessage now)
    |> String.concat "\n"
  | _ -> errorToMessage now error

let private writeQueue = ConcurrentQueue<exn>()

let save = writeQueue.Enqueue

let update = async {
  // let ctx = SynchronizationContext.Current
  do! Async.SwitchToThreadPool()
  while true do
    match writeQueue.TryDequeue() with
    | true, error ->
      let message = toString error
      do! File.AppendAllTextAsync(ErrorLogFile, message) |> Async.AwaitTask
      // do! Async.SwitchToContext ctx
    | _ -> do! Async.Sleep 5
}
