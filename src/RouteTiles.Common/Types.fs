
[<AutoOpen>]
module RouteTiles.Common.Types

[<Struct; RequireQualifiedAccess>]
type Controller =
  | KeyboardShift
  | KeyboardSeparate
  | Joystick of index:int * name:string * guid:string

type RankingData = {
  Name: string
  Time: float32
  Point: int
  SlideCount: int
  TilesCount: int
  RoutesCount: int
  LoopsCount: int
}
