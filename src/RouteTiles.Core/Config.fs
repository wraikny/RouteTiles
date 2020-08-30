namespace RouteTiles.Core

[<Struct; RequireQualifiedAccess>]
type Background =
  | Wave

module Background =
  let items = [|
    Background.Wave
  |]


type Config = {
  guid: System.Guid
  name: string voption
  background: Background
} with
  static member Create() = {
    guid = System.Guid.NewGuid()
    name = ValueNone
    background = Background.Wave
}
