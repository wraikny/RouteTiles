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
  let gameVanishTiles = { path = @"SE/tambourine.ogg"; volumeRate = 0.75f }



[<RequireQualifiedAccess>]
type SoundControlState =
  | Menu
  | Game

open RouteTiles.Core.Utils
open System.Collections.Generic

[<Sealed>]
type SoundControl(bgmVolume, seVolume) =
  inherit Node()

  let fadeSecond = Consts.ViewCommon.BGMFadeSecond

  static let bgmToSound (bgm: BGM) =
    let sound = Sound.LoadStrict(bgm.path, false)
    sound.LoopEndPoint <- bgm.length
    sound

  let gameBGMSounds =
    [|
      BGM.sikoumeikyu
      BGM.rasenkouro
      BGM.hodoku
    |]
    |> Array.map (fun bgm -> bgm, bgmToSound bgm)

  let menuBGMSound =
    let s = bgmToSound BGM.sikoumeikyumaborosi
    s.IsLoopingMode <- true

    BGM.sikoumeikyumaborosi, s

  let seMap =
    [|
      SEKind.CursorMove, SE.cursorMove
      SEKind.Enter, SE.enter
      SEKind.Cancel, SE.cancel
      SEKind.GameMoveCursor, SE.gameMoveCursor
      SEKind.GameMoveTiles, SE.gameMoveTiles
      SEKind.Pause, SE.pause
      SEKind.GameVanishTiles, SE.gameVanishTiles
      // SEKind.Invalid, SE.invalid
    |]
    |> Array.map(fun (k, se) -> (k, (se.volumeRate, Sound.LoadStrict(se.path, true))))
    |> Map.ofSeq

  let mutable playingBGM: (BGM * int) voption = ValueNone
  let mutable fadingInBGM: (BGM * int) voption = ValueNone
  // let playingSEs = ResizeArray<int>()

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

  member __.SetVolume(bgmVol, seVol) =
    bgmVolume <- bgmVol
    seVolume <- seVol
    playingBGM |> ValueOption.iter(fun (bgm, id) ->
      Engine.Sound.SetVolume(id, bgmVolume * bgm.volumeRate)
    )

  member __.PlaySE(kind: SEKind) =
    seMap
    |> Map.tryFind kind
    |> Option.iter (fun (volRate, se) ->
      let id = Engine.Sound.Play se
      Engine.Sound.SetVolume (id, seVolume * volRate)
    )

  member this.SetState(state) =
    state |> function
    | SoundControlState.Menu ->
      let nextId = Engine.Sound.Play (snd menuBGMSound)
      Engine.Sound.SetVolume (nextId, bgmVolume * (fst menuBGMSound).volumeRate)
      Engine.Sound.FadeIn (nextId, fadeSecond)
      fadingInBGM <- ValueSome (fst menuBGMSound, nextId)

      this.StartCoroutine(seq {
        match playingBGM with
        | ValueNone -> ()
        | ValueSome (_, id) ->
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
          fadingInBGM <- ValueSome (nextBGM, nextId)
          Engine.Sound.SetVolume (nextId, bgmVolume * nextBGM.volumeRate)
          Engine.Sound.FadeIn (nextId, fadeSecond)

          count <- (count + 1) % shuffledBGMs.Length

          yield! Coroutine.toParallel [|
            seq {
              yield! Coroutine.sleep (int((nextBGM.length - fadeSecond * 2.0f) * 1000.0f) * 1<millisec>)
            }
            seq {
              match playingBGM with
              | ValueNone -> ()
              | ValueSome (_, id) ->
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
