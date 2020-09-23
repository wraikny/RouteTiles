namespace RouteTiles.App

open Altseed2

type BGM = {
  name: string
  author: string
  path: string
  length: float32
  volumeRate: float32
}

module internal BGM =
  let sikoumeikyu =
    {
      name = "思考迷宮・虚"
      author = "みろく"
      path = @"BGM/03-sikoumeikyu.ogg"
      length = 106.0f
      volumeRate = 0.1f
    }

  let rasenkouro =
    {
      name = "螺旋考路"
      author = "みろく"
      path = @"BGM/04-rasenkouro.ogg"
      length = 110.0f
      volumeRate = 0.1f
    }

  let makukan =
    {
      name = "幕間"
      author = "みろく"
      path = @"BGM/05-makuai.ogg"
      length = 79.0f
      volumeRate = 0.1f
    }

  let hodoku =
    {
      name = "ほどく"
      author = "みろく"
      path = @"BGM/07-hodoku.ogg"
      length = 96.0f
      volumeRate = 0.1f
    }

  let sikoumeikyumaborosi =
    {
      name = "思考迷宮・幻"
      author = "みろく"
      path = @"BGM/08-sikoumeikyumaborosi.ogg"
      length = 150.0f
      volumeRate = 0.1f
    }

  let gameBGMs = [|
    sikoumeikyu
    rasenkouro
    makukan
    hodoku
  |]

[<RequireQualifiedAccess>]
type SEKind =
  | CursorMove
  | Enter
  | Cancel
  | Invalid
  | InputChar
  | DeleteChar
  | GameMoveCursor
  | GameMoveTiles
  | GameVanishTiles
  | Pause
  | ReadyGame
  | StartGame

type SE = {
  path: string
  volumeRate: float32
}

module internal SE =
  let cursorMove = { path = @"SE/select_001_6.ogg"; volumeRate = 0.25f }
  let enter = { path = @"SE/common_001_1.ogg"; volumeRate = 0.25f }
  let cancel = { path = @"SE/select_001_1.ogg"; volumeRate = 0.25f }
  let pause = { path = @"SE/button16.ogg"; volumeRate = 0.25f }
  let gameMoveCursor = { path = @"SE/button45.ogg"; volumeRate = 0.25f }
  let gameMoveTiles = { path = @"SE/button63.ogg"; volumeRate = 0.25f }
  let gameVanishTiles = { path = @"SE/se_maoudamashii_se_sound15.ogg"; volumeRate = 0.5f }
  let readyGame = { path = @"SE/se_maoudamashii_onepoint23.ogg"; volumeRate = 0.25f }
  let startGame = { path = @"SE/se_maoudamashii_onepoint09.ogg"; volumeRate = 0.25f }

  let sePairs = [|
    SEKind.CursorMove, cursorMove
    SEKind.Enter, enter
    SEKind.Cancel, cancel
    SEKind.GameMoveCursor, gameMoveCursor
    SEKind.GameMoveTiles, gameMoveTiles
    SEKind.Pause, pause
    SEKind.GameVanishTiles, gameVanishTiles
    SEKind.ReadyGame, readyGame
    SEKind.StartGame, startGame
    // SEKind.Invalid, SE.invalid
  |]

module SoundControl =

  let loading =
    let bgms = [|
      yield! BGM.gameBGMs
      BGM.sikoumeikyumaborosi
    |]
    
    let ses = SE.sePairs

    (bgms.Length + ses.Length), fun (progress: unit -> int) -> async {
      for bgm in bgms do
        Sound.LoadStrict(bgm.path, false) |> ignore
        progress () |> ignore
      
      for (_, se) in ses do
        Sound.Load(se.path, true) |> ignore
        progress () |> ignore
    }


[<RequireQualifiedAccess>]
type SoundControlState =
  | Menu
  | Game

open RouteTiles.Core.Utils
open System.Collections.Generic

[<Sealed>]
type SoundControl(bgmVolume, seVolume) =
  inherit Node()

  static let fadeSecond = Consts.ViewCommon.BGMFadeSecond

  static let bgmToSound (bgm: BGM) =
    let sound = Sound.LoadStrict(bgm.path, false)
    sound.LoopEndPoint <- bgm.length
    sound

  static let fadeOut (_: BGM, id) = Engine.Sound.FadeOut (id, fadeSecond)

  let gameBGMSounds =
    BGM.gameBGMs
    |> Array.map (fun bgm -> bgm, bgmToSound bgm)

  let menuBGMSound =
    let s = bgmToSound BGM.sikoumeikyumaborosi
    s.IsLoopingMode <- true

    BGM.sikoumeikyumaborosi, s

  let seMap =
    SE.sePairs
    |> Array.map(fun (k, se) -> (k, (se.volumeRate, Sound.LoadStrict(se.path, true))))
    |> Map.ofSeq

  let mutable playingBGM: (BGM * int) voption = ValueNone
  let mutable fadingInBGM: (BGM * int) voption = ValueNone
  let playingGameSEs = ResizeArray<int * float32>()

  let mutable bgmVolume = bgmVolume
  let mutable seVolume = seVolume

  let mutable coroutine: IEnumerator<unit> = null

  let rand = System.Random()

  let shuffleArray (arr: 'a[]) =
    let res = Array.copy arr

    let mutable n = arr.Length
    while n  > 1 do
      n <- n - 1
      let k = rand.Next(n + 1)
      let tmp = arr.[k]
      res.[k] <- res.[n]
      res.[n] <- tmp

    res

  let mutable currentState = ValueNone

  member __.SetVolume(bgmVol, seVol) =
    if seVolume <> seVol then
     seVolume <- seVol
     for (id, volumeRate) in playingGameSEs do
      Engine.Sound.SetVolume(id, seVolume * volumeRate)

    if bgmVolume <> bgmVol then
      bgmVolume <- bgmVol
      let setVol (bgm: BGM, id) = Engine.Sound.SetVolume(id, bgmVolume * bgm.volumeRate)
      playingBGM |> ValueOption.iter setVol
      fadingInBGM |> ValueOption.iter setVol

  member __.PauseSE() =
    for (id, _) in playingGameSEs do
      Engine.Sound.Pause(id)

  member __.ResumeSE() =
    for (id, _) in playingGameSEs do
      Engine.Sound.Resume(id)

  member __.StopSE() =
    for (id, _) in playingGameSEs do
      Engine.Sound.Stop id
    
    playingGameSEs.Clear()

  member __.PlaySE(kind: SEKind, pausable) =
    seMap
    |> Map.tryFind kind
    |> Option.iter (fun (volRate, se) ->
      let id = Engine.Sound.Play se
      Engine.Sound.SetVolume (id, seVolume * volRate)

      if pausable then
        playingGameSEs.Add ((id, volRate))
    )

  member __.State with get() = currentState

  member this.SetState(state) =
    currentState <- ValueSome state
    fadingInBGM |> ValueOption.iter fadeOut

    state |> function
    | SoundControlState.Menu ->
      let nextId = Engine.Sound.Play (snd menuBGMSound)
      Engine.Sound.SetVolume (nextId, bgmVolume * (fst menuBGMSound).volumeRate)

      this.StartCoroutine(seq {
        match playingBGM with
        | ValueNone -> ()
        | ValueSome (_, id) ->
          fadingInBGM <- ValueSome (fst menuBGMSound, nextId)
          Engine.Sound.FadeIn (nextId, fadeSecond)
          Engine.Sound.FadeOut (id, fadeSecond)
          yield! Coroutine.sleep (int (fadeSecond * 1000.0f) * 1<millisec>)

        playingBGM <- ValueSome (fst menuBGMSound, nextId)
        fadingInBGM <- ValueNone
      })

    | SoundControlState.Game ->
      this.StartCoroutine(seq {
        let shuffledBGMs = shuffleArray gameBGMSounds

        let mutable count = 0
        while true do
          // 次の曲を指定
          let (nextBGM, nextBGMSound) = shuffledBGMs.[count]
          let nextId = Engine.Sound.Play nextBGMSound
          Engine.Sound.SetVolume (nextId, bgmVolume * nextBGM.volumeRate)

          count <- (count + 1) % shuffledBGMs.Length

          yield! Coroutine.toParallel [|
            seq {
              yield! Coroutine.sleep (int((nextBGM.length - fadeSecond) * 1000.0f) * 1<millisec>)
            }
            seq {
              match playingBGM with
              | ValueNone -> ()
              | ValueSome (_, id) ->
                fadingInBGM <- ValueSome (nextBGM, nextId)
                Engine.Sound.FadeIn (nextId, fadeSecond)
                Engine.Sound.FadeOut (id, fadeSecond)
                yield! Coroutine.sleep (int (fadeSecond * 1000.0f) * 1<millisec>)

              playingBGM <- ValueSome (nextBGM, nextId)
              fadingInBGM <- ValueNone
            }
          |]
      })

  member private __.StartCoroutine(s: seq<unit>) =
    coroutine <- s.GetEnumerator()

  override __.OnUpdate() =
    if coroutine <> null then
      if not (coroutine.MoveNext()) then
        coroutine <- null
        
    let mutable index = 0
    while index < playingGameSEs.Count do
      let id, _ = playingGameSEs.[index]
      if not <| Engine.Sound.GetIsPlaying(id) then
        playingGameSEs.RemoveAt(index)
      index <- index + 1
