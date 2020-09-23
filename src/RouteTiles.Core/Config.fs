namespace RouteTiles.Core

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
