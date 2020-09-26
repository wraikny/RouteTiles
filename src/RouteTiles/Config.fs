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

let [<Literal>] ConfigFile = @"Data/config.bin"

let dirName = Path.GetDirectoryName ConfigFile


let private aes =
  new AesManaged
    ( KeySize = 256
    , BlockSize = 128
    , Mode = CipherMode.CBC
    , IV = Encoding.UTF8.GetBytes(ResourcesPassword.iv)
    , Key = Encoding.UTF8.GetBytes(ResourcesPassword.key)
    , Padding = PaddingMode.PKCS7
    )


let private write (conf: Config) =
  let exists = Directory.Exists(dirName)
  if not exists then
    Directory.CreateDirectory(dirName) |> ignore

  let content = Json.serialize conf
  let byteText = Encoding.UTF8.GetBytes content
  let encryptText = aes.CreateEncryptor().TransformFinalBlock(byteText, 0, byteText.Length)

  File.WriteAllBytesAsync(ConfigFile, encryptText)
  |> Async.AwaitTask

let private read () = async {
  let! encryptText =
    File.ReadAllBytesAsync ConfigFile
    |> Async.AwaitTask

  let byteText = aes.CreateDecryptor().TransformFinalBlock(encryptText, 0, encryptText.Length)

  let content = Encoding.UTF8.GetString byteText

  return Json.deserialize<Config> content
}

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
      match! write conf |> Async.Catch with
      | Choice1Of2 () -> Utils.DebugLogn "Succeeded to write config"
      | Choice2Of2 e ->
        Utils.DebugLogn (sprintf "%O" e)
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
    match! read () |> Async.Catch with
    | Choice1Of2 conf ->
      config <- ValueSome conf
      return conf
    | Choice2Of2 e ->
      Utils.DebugLogn (sprintf "%O" e)
      return createNew()
  else
    return createNew()
}
