module internal RouteTiles.App.Menu.MenuElement

open System
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Menu
open RouteTiles.Menu.SubMenu
open RouteTiles.Menu.Types
open RouteTiles.Menu.Effects
open RouteTiles.App
open RouteTiles.App.BoxUIElements
open RouteTiles.App.Menu.ElementCommon

let leftArea() =
  FixedArea.Create(RectF(0.0f, 0.0f, 445.0f, 720.0f))
  :> Element

let rightArea() =
  FixedArea.Create(RectF(445.0f + 120.0f, 0.0f, 1280.0f - 445.0f - 120.0f, 720.0f))
  :> Element

let private createTitle (container: Container) =
  Sprite.Create
    ( aspect = Aspect.Fixed
    , texture = container.TitleTexture
    , zOrder = ZOrder.Menu.title
    )
  |> BoxUI.marginTop (LengthScale.Fixed, 80.0f)
  |> BoxUI.debug
  :> Element

let private buttonZOrders =
  {|button = ZOrder.Menu.buttonBackground
    buttonText = ZOrder.Menu.buttonText
  |}

let private createButtons (container: Container) (selections: string[]) param =
  leftArea()
  |> BoxUI.debug
  |> BoxUI.withChild (
    empty()
    |> BoxUI.marginBottom (LengthScale.Fixed, 80.0f)
    |> BoxUI.marginLeft (LengthScale.Fixed, 40.0f)
    |> BoxUI.withChild(
      buttons buttonZOrders (32.0f, true, false) container selections param
      |> BoxUI.alignX Align.Min
      |> BoxUI.alignY Align.Max
    )
    |> BoxUI.debug
  )

let private createDesc (container: Container) text =
  empty()
  |> BoxUI.marginBottom (LengthScale.Fixed, 80.0f)
  |> BoxUI.withChild(
    Text.Create
      ( text = text
      , font = container.Font
      , fontSize = 32.0f
      , zOrder = ZOrder.Menu.description
      )
    |> BoxUI.alignY Align.Max
    |> BoxUI.debug
  )

let createCurrentMode zOrder (container: Container) (text: string) =
  empty ()
  |> BoxUI.marginLeft (LengthScale.Fixed, 40.0f)
  |> BoxUI.marginTop (LengthScale.Fixed, 40.0f)
  |> BoxUI.withChild (
    Text.Create
      ( text = text
      , font = container.Font
      , fontSize = 32.0f
      , zOrder = zOrder
      )
    |> BoxUI.debug
  )

let createCurrentModeMenu = createCurrentMode ZOrder.Menu.currentMode

let private createMainMenu (container: Container) (state: ListSelector.State<Menu.Mode>) =
  [|
    createBackground container
    createButtons container container.MainMenuButtons (state.cursor, state.current)

    rightArea()
    |> BoxUI.withChildren [|
      createTitle container
      createDesc container container.MainMenuDescriptions.[state.cursor]
    |]
  |]

let private createGamemodeSelect (container: Container) (state: ListSelector.State<GameMode>) =
  [|
    createBackground container

    createCurrentModeMenu container container.TextMap.modes.gameModeSelect

    createButtons container container.GameModeButtons (state.cursor, state.current)
    rightArea().With(createDesc container container.GameModeDescriptions.[state.cursor])
  |]

let private createSetting (container: Container) (state: Setting.State) =
  state |> function
  | Setting.Base { selector = selector } ->
    [|
      createBackground container

      createCurrentModeMenu container container.TextMap.modes.setting

      createButtons container container.SettingMenuButtons (selector.cursor, selector.current)
      rightArea().With(createDesc container container.SettingModeDescriptions.[selector.cursor])
    |]
  | Setting.State.InputName _ ->
    failwithf "Invalid State: %A" state

  | _ -> Array.empty




let private createBlur zo1 zo2 =
  [|
    GaussianBlur.Create(intensity = 5.0f, zOrder = zo1) :> Element
    Rectangle.Create
      ( zOrder = zo2
      , color = Consts.Menu.blurDarkColor
      )
    :> Element
  |]

let private createBlurDefault () = createBlur ZOrder.Menu.blur ZOrder.Menu.darkMask
let private createBlurOverGameInfo () = createBlur ZOrder.Menu.blurOverGameInfo ZOrder.Menu.darkMaskOverGameInfo

let createPause (container: Container) (state: Pause.State) =
  [|
    yield createBackground container

    yield createCurrentModeMenu container container.TextMap.modes.pause

    yield! createBlurDefault ()
    match state with
    | Pause.Base (_, selector) ->
      yield createButtons container container.PauseModeButtons (selector.cursor, selector.current)
      yield rightArea().With(createDesc container container.PauseModeDescriptions.[selector.cursor])
    | _ -> ()
  |]

let private createControllerSelect =
  controllerSelect {| buttonZOrders with desc = ZOrder.Menu.description; background = ZOrder.Menu.frameBackground |}


let rankingFrameSize = Vector2F(1120.0f, 88.0f)

let private createRankingList (container: Container) (state: Ranking.State) =
  ItemList.Create()
  |> BoxUI.alignCenter
  |> BoxUI.withChildren (
    let makeTextRaw font fontize alignX pos color text =
      let textElem =
        Text.Create
          ( font = font
          , fontSize = fontize
          , text = text
          , color = Nullable(color)
          , zOrder = ZOrder.Menu.buttonText
          )
        |> BoxUI.alignY Align.Center
        |> BoxUI.alignX alignX

      (FixedWidth.Create (0.0f)
      |> BoxUI.marginLeft (LengthScale.Fixed, pos)
      |> BoxUI.withChild textElem
      :> Element)
      , textElem

    // let makeText = makeTextRaw container.MonoFont
    let makeText = makeTextRaw container.Font 32.0f

    let firstIndex = Ranking.OnePageItemCount * state.page

    // data
    // |> fun a -> if a.Length > 5 then a.[0..4] else a
    [| firstIndex .. firstIndex + Ranking.OnePageItemCount - 1 |]
    |> Array.map (fun i ->
      Sprite.Create
        (
          texture = container.RankingFrame
        , src = Nullable(RectF(Vector2F(0.0f, 0.0f), rankingFrameSize))
        , zOrder = ZOrder.Menu.buttonBackground
        , aspect = Aspect.Fixed
        )
      |> BoxUI.withChildren
        [|
          let white = Color(255uy, 255uy, 255uy)
          match state.data |> Array.tryItem i with
          | None ->
            let elem1, _textElem1 = makeText Align.Center 56.0f white (sprintf "%d" <| i + 1)
            elem1

          | Some data ->
            let color =
              if state.insertedId = ValueSome data.id then Color(255uy, 255uy, 100uy)
              elif state.config.guid = data.userId then Color(100uy, 100uy, 255uy)
              else white
            
            let elem1, textElem1 = makeText Align.Center 56.0f color (sprintf "%d" <| i + 1)
            elem1
            
            let elem2, textElem2 = makeText Align.Center 272.0f color data.values.Name
            elem2

            match state.gameMode with
            | GameMode.ScoreAttack180 -> sprintf "%d pt." data.values.Point
            | _ -> secondToDisplayTime data.values.Time
            |> replaceOne
            |> makeText Align.Max 728.0f (Color(0uy, 0uy, 0uy))
            |> fst

            
            empty ()
            |> BoxUI.marginRight (LengthScale.Fixed, 20.0f)
            |> BoxUI.withChild (
              Text.Create
                ( font = container.Font
                , fontSize = 32.0f
                , text = (data.utcDate.ToLocalTime().ToString("yyyy/MM/dd HH:mm") |> replaceOne)
                , color = Nullable(Color(0uy, 0uy, 0uy))
                , zOrder = ZOrder.Menu.buttonText
                )
              |> BoxUI.alignY Align.Center
              |> BoxUI.alignX Align.Max
            )

            if state.insertedId = ValueSome data.id then
              Sprite.Create
                (
                  texture = container.RankingFrame
                , src = Nullable(RectF(Vector2F(0.0f, rankingFrameSize.Y), rankingFrameSize))
                , zOrder = ZOrder.Menu.buttonBackground
                , aspect = Aspect.Fixed
                )
              |> fun e ->
                BoxUISystem.Post (fun () ->
                  let startTime = Engine.Time
                  e.add_OnUpdateEvent(fun node ->
                    let dTime = Engine.Time - startTime
                    let sinTime = cos (dTime * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)

                    let a = if sinTime > 0.0f then 1.0f else 0.0f
                    node.Color <- Color (255, 255, 255, a * 255.0f |> int)

                    let colB = 255.0f * (1.0f - a) |> int

                    textElem1.Node.Color <- Color (255, 255, colB, 255)
                    textElem2.Node.Color <- Color (255, 255, colB, 255)
                  )
                )
                e
        |]
        
    )
    |> fun x ->
      let makeMargin height =
        FixedSize.Create(Vector2F(rankingFrameSize.X, height)) :> Element

      let createArrow isUp isDrawn =
        FixedSize.Create(Vector2F(rankingFrameSize.X, moreArrowSize.Y)).With(
          createArrow container ZOrder.Menu.buttonBackground isUp isDrawn 
          |> BoxUI.alignX Align.Center
        ) :> Element

      [|
        createArrow true state.IsDecrementable
        makeMargin 12.0f
        for i in 0..3 do
          x.[i] :> Element
          makeMargin 20.0f
        x.[4]

        makeMargin 12.0f
        createArrow false state.IsIncrementable
      |]
  )


let private createGameResult (container: Container) state =
  state |> function
  | GameResult.ResultWithSendToServerSelectState(_, _, _, selector) ->
    [|
      // createBackground container
      yield! createBlurDefault ()
      createCurrentModeMenu container container.TextMap.modes.gameResult
      createButtons container container.GameResultSendToServerButtons (selector.cursor, selector.current)
      rightArea().With(createDesc container container.TextMap.descriptions.sendToServer)
    |]

  | GameResult.GameNextSelectionState(selector, _) ->
    [|
      // createBackground container
      yield! createBlurDefault ()
      createCurrentModeMenu container container.TextMap.modes.gameResult
      createButtons container container.GameResultNextSelectionButtons (selector.cursor, selector.current)
      rightArea().With(createDesc container container.GameResultNextSelectionDescriptions.[selector.cursor])
    |]

  | GameResult.RankingListViewState(state, _) ->
    [|
      yield! createBlurOverGameInfo ()
      createCurrentMode ZOrder.Menu.currentModeOverGameInfo container container.RankingGameMode.[state.gameMode]
      createRankingList container state
    |]

  | _ -> Array.empty


let create (container: Container) (state: Menu.State) =
  createBase()
  |> BoxUI.withChildren (
    state |> function
    | Menu.State.MainMenuState (_, state) ->
      createMainMenu container state

    | Menu.State.GameModeSelectState (WithContext state, _) ->
      createGamemodeSelect container state

    | Menu.State.SettingMenuState (state, _) ->
      createSetting container state

    | Menu.State.ControllerSelectState (WithContext(Menu.ControllerSelectToPlay state), _) ->
      [|
        createBackground container
        createCurrentModeMenu container container.TextMap.modes.controllerSelect
        yield!
          createControllerSelect container state
      |]

    | Menu.State.PauseState(state, _) ->
      createPause container state

    | Menu.State.GameResultState(state, _) ->
      createGameResult container state

    | Menu.State.RankingState(state, _) ->
      [|
        createBackground container
        createCurrentMode ZOrder.Menu.currentModeOverGameInfo container container.RankingGameMode.[state.gameMode]
        createRankingList container state
      |]

    | _ -> Array.empty
  )
