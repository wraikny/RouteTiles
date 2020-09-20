namespace RouteTiles.App

open Altseed2

type BGM = {
  name: string
  author: string
  path: string
  length: float32
}

module internal BGM =
  let sikoumeikyu =
    {
      name = "思考迷宮・虚"
      author = "みろく"
      path = @"BGM/03-sikoumeikyu.ogg"
      length = 106.0f
    }

  let rasenkouro =
    {
      name = "螺旋考路"
      author = "みろく"
      path = @"BGM/04-rasenkouro.ogg"
      length = 110.0f
    }

  let hodoku =
    {
      name = "ほどく"
      author = "みろく"
      path = @"BGM/07-hodoku.ogg"
      length = 96.0f
    }

  let sikoumeikyumaborosi =
    {
      name = "思考迷宮・幻"
      author = "みろく"
      path = @"BGM/08-sikoumeikyumaborosi.ogg"
      length = 150.0f
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

module internal SE =
  let [<Literal>] cursorMove = @"SE/select_001_6.wav"
  let [<Literal>] enter = @"SE/common_001_1.wav"
  let [<Literal>] cancel = @"SE/select_001_1.wav"
  let [<Literal>] gameMoveTiles = @"SE/button63.wav"
  



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

  static let bgmToSound bgm =
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
      SEKind.GameMoveTiles, SE.gameMoveTiles
      // SEKind.Invalid, SE.invalid
    |]
    |> Array.map(fun (k, path) -> (k, Sound.LoadStrict(path, true)))
    |> Map.ofSeq

  let mutable playingBGM = ValueNone
  let mutable fadingInBGM = ValueNone
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

  // member __.CurrentBGM =
  //   playingBGM
  //   |> ValueOption.map fst

  member __.SetVolume(bgmVolume', seVolume') =
    bgmVolume <- bgmVolume'
    seVolume <- seVolume'

    playingBGM |> ValueOption.iter(fun (_, id) ->
      Engine.Sound.SetVolume(id, bgmVolume)
    )

  member __.PlaySE(kind: SEKind) =
    seMap
    |> Map.tryFind kind
    |> Option.iter (fun se ->
      let id = Engine.Sound.Play se
      Engine.Sound.SetVolume (id, seVolume)
    )

  member this.SetState(state) =
    state |> function
    | SoundControlState.Menu ->
      let nextId = Engine.Sound.Play (snd menuBGMSound)
      Engine.Sound.SetVolume (nextId, bgmVolume)
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
          Engine.Sound.SetVolume (nextId, bgmVolume)
          Engine.Sound.FadeIn (nextId, fadeSecond)

          count <- (count + 1) % shuffledBGMs.Length

          yield! Coroutine.toParallel [|
            seq {
              yield! Coroutine.sleep (int((nextBGM.length - fadeSecond) * 1000.0f) * 1<millisec>)
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
