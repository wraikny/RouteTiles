
[<AutoOpen>]
module RouteTiles.Core.Types.Common

[<Struct; RequireQualifiedAccess>]
type Dir = Up | Down | Right | Left

[<Struct; RequireQualifiedAccess>]
type Controller =
  | Keyboard
  | Joystick of index:int * name:string * guid:string


open Affogato

module Dir =
  let dirs = [| Dir.Up; Dir.Down; Dir.Right; Dir.Left |]

  let inline rev dir = dir |> function
    | Dir.Up -> Dir.Down
    | Dir.Right -> Dir.Left
    | Dir.Down -> Dir.Up
    | Dir.Left -> Dir.Right

  let inline toVector (dir: Dir) =
    let (a, b) = dir |> function
      | Dir.Up -> (0, -1)
      | Dir.Down -> (0, 1)
      | Dir.Right -> (1, 0)
      | Dir.Left -> (-1, 0)

    Vector2.init a b

  let isVertical = function | Dir.Up | Dir.Down -> true | _ -> false