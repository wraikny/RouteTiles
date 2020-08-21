module RouteTiles.App.MenuView

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core.Types
open RouteTiles.Core.Types.Menu
open RouteTiles.App.BoxUIElements
open RouteTiles.App.Menu.Common
open RouteTiles.App.Menu

let private window() =
  Window.Create() :> ElementRoot

let menu (model: Model) =
  model.state |> function
  | State.Game _ -> window()

  | State.PauseGame (_, _, index) ->
    window().With(Pause.element index)

  | State.Menu ->
    window().With(Main.element model)

  | State.GameSetting (gameMode, setting) ->
    window().With
      (GameSetting.element (gameMode, setting))

  | _ -> window()


let initialize =
  let texts =[|
    for x in Main.modeTexts -> x.Value
    for x in GameSetting.TimeAttack.modeDescs -> x.Value
    for x in GameSetting.ScoreAttack.modeDescs -> x.Value
  |]

  let names =
    texts
    |> Seq.map(fun t -> t.name.ToCharArray())
    |> Seq.concat
    |> Seq.distinct
    |> Seq.toArray

  let descs =
    texts
    |> Seq.map(fun t -> t.desc.ToCharArray())
    |> Seq.concat
    |> Seq.distinct
    |> Seq.toArray

  let otherCharacters = [|
    for i in 'a'..'z' -> i
    for i in 'A'..'Z' -> i
    yield! "点分ゲームスタート"
  |]

  let progressSum =
    [|names.Length
      descs.Length
      otherCharacters.Length
      2 // font
    |] |> Array.sum

  progressSum, fun (progress: unit -> int) -> async {
    let ctx = SynchronizationContext.Current
    do! Async.SwitchToThreadPool()

    let fontName = fontName()
    progress() |> ignore
    let fontDesc = fontDesc()
    progress() |> ignore

    do! Async.SwitchToContext(ctx)

    let Step = 10
    for c in names do
      fontName.GetGlyph(int c) |> ignore
      if progress() % Step = 0 then
        do! Async.Sleep 1

    for c in descs do
      if c <> '\n' then
        fontDesc.GetGlyph(int c) |> ignore
        if progress() % Step = 0 then
          do! Async.Sleep 1

    for c in otherCharacters do
      fontDesc.GetGlyph(int c) |> ignore
      if progress() % Step = 0 then
        do! Async.Sleep 1
  }