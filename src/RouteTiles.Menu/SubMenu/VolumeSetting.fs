module RouteTiles.Menu.SubMenu.VolumeSetting

open RouteTiles.Common.Utils
open RouteTiles.Menu
open RouteTiles.Menu.Effects
open RouteTiles.Menu.SubMenu

open EffFs
open EffFs.Library.StateMachine

[<RequireQualifiedAccess>]
type VolumeMode = BGM | SE

module VolumeMode =
  let items = [| VolumeMode.BGM; VolumeMode.SE |]

let [<Literal>] VolumeMin = 0
let [<Literal>] VolumeMax = 10

type State = {
  bgmVolume: int
  seVolume: int
  cursor: int
} with
  static member Init(bgmVolume, seVolume) =
    { bgmVolume = bgmVolume * 10.0f |> int
      seVolume = seVolume * 10.0f |> int
      cursor = 0
    }

  static member StateOut(_) = Eff.marker<(float32 * float32) voption>

let volumeIntToFloat32 x = float32 x / 10.0f

[<Struct; RequireQualifiedAccessAttribute>]
type Msg =
  | Enter
  | Cancel
  | Incr
  | Decr
  | Right
  | Left

let toFloat32Volumes ({ bgmVolume = bgmVolume; seVolume = seVolume }: State) =
  (float32 bgmVolume / 10.0f, float32 seVolume / 10.0f)

let setVolumeEffect (state: State) =
  SetSoundVolume <| toFloat32Volumes state

let inline update msg state = eff {
  match msg with
  | Msg.Enter ->
    do! SoundEffect.Select
    return toFloat32Volumes state |> ValueSome |> Completed
  | Msg.Cancel ->
    do! SoundEffect.Cancel
    return Completed ValueNone
  | Msg.Incr | Msg.Decr ->
    let cursor = state.cursor + (if msg = Msg.Incr then +1 else -1) |> clamp 0 (VolumeMode.items.Length - 1)
    if cursor = state.cursor then
      do! SoundEffect.Invalid
      return Pending state
    else
      do! SoundEffect.Move
      return Pending { state with cursor = cursor }

  | Msg.Right | Msg.Left ->
    let f x = x + (if msg = Msg.Right then +1 else -1) |> clamp VolumeMin VolumeMax

    match VolumeMode.items.[state.cursor] with
    | VolumeMode.BGM ->
      let v = f state.bgmVolume
      if v = state.bgmVolume then
        do! SoundEffect.Invalid
        return Pending state
      else
        let state = { state with bgmVolume = f state.bgmVolume }
        do! setVolumeEffect state
        do! SoundEffect.Move
        return Pending state

    | VolumeMode.SE ->
      let v = f state.seVolume
      if v = state.seVolume then
        do! SoundEffect.Invalid
        return Pending state
      else
        let state = { state with seVolume = f state.seVolume }
        do! setVolumeEffect state
        do! SoundEffect.Move
        return Pending state

}