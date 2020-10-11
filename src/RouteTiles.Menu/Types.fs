module RouteTiles.Menu.Types

[<Struct; RequireQualifiedAccess>]
type Background =
  | Wave
  | FloatingTiles

module Background =
  let items = [|
    Background.FloatingTiles
    Background.Wave
  |]


type Config = {
  guid: System.Guid
  name: string voption
  background: Background
  bgmVolume: float32
  seVolume: float32
} with
  static member Create() = {
    guid = System.Guid.NewGuid()
    name = ValueNone
    background = Background.FloatingTiles
    bgmVolume = 0.5f
    seVolume = 0.5f
}


[<Struct>]
type GameMode =
  | TimeAttack5000
  | ScoreAttack180
  | Endless

module GameMode =
  let items = [|
    ScoreAttack180
    TimeAttack5000
    Endless
  |]

  let selected = ScoreAttack180

//   let into = function
//     | Endless -> Mode.Endless
// #if DEBUG
//     | TimeAttack5000 -> Mode.TimeAttack 5000
//     | ScoreAttack180 -> Mode.ScoreAttack 180
// #else
//     | TimeAttack5000 -> Mode.TimeAttack 5000
//     | ScoreAttack180 -> Mode.ScoreAttack 180
// #endif
