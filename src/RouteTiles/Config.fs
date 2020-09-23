module RouteTiles.App.Config

open RouteTiles.Core

open System
open System.Threading
open System.IO
open System.Text
open System.Collections.Concurrent
// open System.Runtime.Serialization
// open System.Runtime.Serialization.Formatters.Binary
open System.Security.Cryptography

open FSharp.Json

let [<Literal>] ConfigFile = @"Data/config.json"

let dirName = Path.GetDirectoryName ConfigFile


let private aes = new AesManaged()


let private write (conf: Config) =
  let exists = Directory.Exists(dirName)
  if not exists then
    Directory.CreateDirectory(dirName) |> ignore

  let content = Json.serialize conf

  File.WriteAllTextAsync(ConfigFile, content)
  |> Async.AwaitTask

let private writeQueue = ConcurrentQueue<Config>()

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
      do! write conf
      lock lockObj (fun () -> config <- ValueSome conf)
      // do! Async.SwitchToContext ctx
    | _ -> do! Async.Sleep 100
}

let initialize = async {
  do! Async.SwitchToThreadPool()

  let exists = Directory.Exists(dirName)
  if not exists then
    Directory.CreateDirectory(dirName) |> ignore

  let fileExists = File.Exists(ConfigFile)

  let createNew() =
    let config' = Config.Create()
    save config'
    config <- ValueSome config'
    config'
  
  if fileExists then
    try
      let! fileText =
        File.ReadAllTextAsync ConfigFile
        |> Async.AwaitTask

      let res = Json.deserialize<Config> fileText
      config <- ValueSome res
      return res
    with _ ->
      return createNew()
  else
    return createNew()
}
