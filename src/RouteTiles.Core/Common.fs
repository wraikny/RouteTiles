namespace RouteTiles.Core

open Affogato

[<Struct; RequireQualifiedAccess>]
type Dir = Up | Down | Right | Left

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

[<Struct; CustomEquality; NoComparison>]
type SetOf2<'a when 'a : equality> = private | SetOf2 of 'a * 'a

with
  override x.Equals(yobj) = 
    match yobj with
    | :? SetOf2<'a> as y ->
      match x, y with
      | SetOf2(xa, xb), SetOf2(ya, yb) when (xa = ya && xb = yb) || (xa = yb && xb = ya) -> true
      | _ -> false
    | _ -> false

    override x.GetHashCode() =
      x |> function
      | SetOf2(a, b) ->
        hash a ||| hash b

    interface System.IEquatable<SetOf2<'a>> with
      member this.Equals(that : SetOf2<'a>) =
          this.Equals(that)

[<Struct; RequireQualifiedAccess>]
type Controller =
  | Keyboard
  | Joystick of name:string * index:int
