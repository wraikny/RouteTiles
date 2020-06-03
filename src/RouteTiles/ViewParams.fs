namespace RouteTiles.App

open RouteTiles.Core.Utils

open Altseed
open Affogato

module Consts =

  let nextsCount = 3
  let boardSize = Vector2.init 4 5

  let nextsPos = Vector2F(550.0f, 50.0f)
  let nextsScale = 0.8f

  let tilesPos = Vector2F(50.f, 120.0f)
  let tileSize = Vector2F(100.0f, 100.0f)
  let tileMergin = Vector2F(10.0f, 10.0f)

  let backGroundColor = Color(100, 100, 100, 255)

  let tileSlideInterval = 200<milisec>

module ZOrder =
  let board = (|||) (10 <<< 16)
