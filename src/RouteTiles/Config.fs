module RouteTiles.App.Config

open RouteTiles.Core

open System
open System.Threading
open System.IO
open System.Collections.Concurrent
open System.Runtime.Serialization
open System.Runtime.Serialization.Formatters.Binary

let [<Literal>] ConfigFile = @"Data/config.bat"

let dirName = Path.GetDirectoryName ConfigFile
let formatter = new BinaryFormatter()

let private write (conf: Config) =
  let exists = Directory.Exists(dirName)
  if not exists then
    Directory.CreateDirectory(dirName) |> ignore

  use file = new FileStream(ConfigFile, FileMode.Create)
  formatter.Serialize(file, conf)

let private writeQueue = ConcurrentQueue<Config>()

let save = writeQueue.Enqueue

let update = async {
  let ctx = SynchronizationContext.Current
  while true do
    match writeQueue.TryDequeue() with
    | true, conf ->
      do! Async.SwitchToThreadPool()
      write conf
      do! Async.SwitchToContext ctx
    | _ -> do! Async.Sleep 100
}

let mutable private config = ValueNone
let tryGetConfig() = config

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
      use file = new FileStream(ConfigFile, FileMode.OpenOrCreate)
      let res = formatter.Deserialize(file) |> unbox<Config>
      config <- ValueSome res
      return res
    with :? SerializationException ->
      return createNew()
  else
    return createNew()
}
