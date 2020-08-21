module RouteTiles.App.BoxUI

open Altseed2
open Altseed2.BoxUI


let inline withChild c (e: #Element) = e.With(c)

let inline withChildren (cs: 'xs) (e: #Element) =
  for c in cs do e.AddChild(c)
  e

let inline marginLeft x (e: #Element) = e.MarginLeft <- x; e
let inline marginRight x (e: #Element) = e.MarginRight <- x; e
let inline marginTop x (e: #Element) = e.MarginTop <- x; e
let inline marginBottom x (e: #Element) = e.MarginBottom <- x; e

let inline marginX x e = e |> marginLeft x |> marginRight x
let inline marginY y e = e |> marginTop y |> marginBottom y
let inline marginXY (ls, x, y) e = e |> marginX (ls, x) |> marginY (ls, y)

let inline margin margin e = e |> marginX margin |> marginY margin

let inline alignX x (e: #Element) = e.AlignX <- x; e
let inline alignY y (e: #Element) = e.AlignY <- y; e
let inline alignCenter (e: #Element) =
  e.AlignX <- Align.Center
  e.AlignY <- Align.Center
  e

let inline onUpdate f (e: ^e) =
  (^e: (member add_OnUpdateEvent: System.Action<'n> -> unit) e, System.Action<_>(f))
  e
