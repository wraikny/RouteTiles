module RouteTiles.App.Affogato

open Affogato
open Altseed2

type Translater =
  static member inline ToAffogato(v: Vector2F) = Vector2.init v.X v.Y
  static member inline ToAffogato(v: Vector2I) = Vector2.init v.X v.Y
  static member inline ToAffogato(v: Vector3F) = Vector3.init v.X v.Y
  static member inline ToAffogato(v: Vector3I) = Vector3.init v.X v.Y
  static member inline ToAffogato(v: Vector4F) = Vector4.init v.X v.Y
  static member inline ToAffogato(v: Vector4I) = Vector4.init v.X v.Y

  static member inline FromAffogato(v: _ Vector2) = Vector2F(v.x, v.y)
  static member inline FromAffogato(v: _ Vector2) = Vector2I(v.x, v.y)
  static member inline FromAffogato(v: _ Vector3) = Vector3F(v.x, v.y, v.z)
  static member inline FromAffogato(v: _ Vector3) = Vector3I(v.x, v.y, v.z)
  static member inline FromAffogato(v: _ Vector4) = Vector4F(v.x, v.y, v.z, v.w)
  static member inline FromAffogato(v: _ Vector4) = Vector4I(v.x, v.y, v.z, v.w)


let inline toAffogato (v: ^va): 'vb =
  let inline f (_impl: ^I, x: ^a) = ((^I or ^a): (static member ToAffogato:_->_)x)
  f (Unchecked.defaultof<Translater>, v)

let inline fromAffogato (v: ^va): 'vb =
  let inline f (_impl: ^I, x: ^a) = ((^I or ^a): (static member FromAffogato:_->_)x)
  f (Unchecked.defaultof<Translater>, v)
