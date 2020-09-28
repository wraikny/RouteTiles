module RouteTiles.Core.Types.Board

open RouteTiles.Core.Types
open Affogato

[<Measure>]type TileId

[<Struct; RequireQualifiedAccess>]
type LineState =
  | Default
  | Routing
  | Looping
  | Routed
  | Looped

[<Struct; RequireQualifiedAccess>]
type RouteState =
  | Single of single:LineState
  | Cross of horizontal:LineState * vertical:LineState
  | Empty

[<Struct; RequireQualifiedAccess>]
type RouteOrLoop =
  | Route of route:(int Vector2 * int<TileId>)[]
  | Loop of loop:(int Vector2 * int<TileId>)[]
with
  member x.Value = x |> function
    | Route r -> r
    | Loop l -> l

[<Struct; RequireQualifiedAccess>]
type TileDir =
  | UpRight
  | UpDown
  | UpLeft
  | RightDown
  | RightLeft
  | DownLeft
  | Cross
  | Empty

[<Struct>]
type Tile = {
  id: int<TileId>
  dir: TileDir
  routeState: RouteState
}

type BoardConfig = {
  size: int Vector2
  nextCounts: int
}


type Model = {
  config: BoardConfig
  markers: (int * int * Dir)[]

  slideCount: int

  nextId: int<TileId>
  cursor: int Vector2
  tiles: Tile voption [,]
  nextTiles: Tile list * Tile list

  point: int

  routesAndLoops: (Set<RouteOrLoop> * int) voption
  routesHistory: int<TileId>[] list
  loopsHistory: int<TileId>[] list
  vanishedTilesCount: int
}