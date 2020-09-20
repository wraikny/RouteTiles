module internal RouteTiles.App.MenuV2.MenuElement

open System
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core
open RouteTiles.Core.SubMenu
open RouteTiles.Core.Types
open RouteTiles.Core.Effects
open RouteTiles.App
open RouteTiles.App.BoxUIElements
open RouteTiles.App.MenuV2.ElementCommon

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
      , zOrder = zOrder
      )
    |> BoxUI.debug
  )

let createCurrentModeMenu = createCurrentMode ZOrder.Menu.currentMode

let private createMainMenu (container: Container) (state: ListSelector.State<MenuV2.Mode>) =
  [|
    createBackground container
    createButtons container container.MainMenuButtons (state.cursor, state.current)

    rightArea()
    |> BoxUI.withChildren [|
      createTitle container
      createDesc container container.MainMenuDescriptions.[state.cursor]
    |]
  |]

let private createGamemodeSelect (container: Container) (state: ListSelector.State<SoloGame.GameMode>) =
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


let private createRankingList (container: Container) (config: Config) (gameMode: SoloGame.GameMode) (id: int64 voption) (data: SimpleRankingsServer.Data<Ranking.Data>[]) =
  ItemList.Create(itemMargin = 20.0f)
  |> BoxUI.withChildren (
    let makeText alignX pos color text =
      let textElem =
        Text.Create
          ( font = container.Font
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

    let texSize = Vector2F(1120.0f, 88.0f)

    // data
    // |> fun a -> if a.Length > 5 then a.[0..4] else a
    [| 0..4 |]
    |> Array.map (fun i ->
      Sprite.Create
        (
          texture = container.RankingFrame
        , src = Nullable(RectF(Vector2F(0.0f, 0.0f), texSize))
        , zOrder = ZOrder.Menu.buttonBackground
        , aspect = Aspect.Fixed
        )
      |> BoxUI.withChildren
        [|
          match data |> Array.tryItem i with
          | None -> ()
          | Some data ->
            let color =
              if id = ValueSome data.id then Color(255uy, 255uy, 100uy)
              elif config.guid = data.userId then Color(100uy, 100uy, 255uy)
              else Color(255uy, 255uy, 255uy)
            
            let elem1, textElem1 = makeText Align.Center 56.0f color (sprintf "%d" i)
            elem1
            
            let elem2 ,textElem2 = makeText Align.Center 272.0f color data.values.Name
            elem2

            match gameMode with
            | SoloGame.GameMode.ScoreAttack180 -> sprintf "%d pt." data.values.Point
            | _ -> secondToDisplayTime data.values.Time
            |> makeText Align.Max 728.0f (Color(0uy, 0uy, 0uy))
            |> fst

            
            empty ()
            |> BoxUI.marginRight (LengthScale.Fixed, 20.0f)
            |> BoxUI.withChild (
              Text.Create
                ( font = container.Font
                , text = data.utcDate.ToLocalTime().ToString("yyyy/MM/dd hh:mm")
                , color = Nullable(Color(0uy, 0uy, 0uy))
                , zOrder = ZOrder.Menu.buttonText
                )
              |> BoxUI.alignY Align.Center
              |> BoxUI.alignX Align.Max
            )

            if id = ValueSome data.id then
              
              Sprite.Create
                (
                  texture = container.RankingFrame
                , src = Nullable(RectF(Vector2F(0.0f, texSize.Y), texSize))
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
  )
  |> BoxUI.alignCenter



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

  | GameResult.RankingListViewState(SinglePage.SinglePageState (config, gameMode, id, data), _) ->
    [|
      yield! createBlurOverGameInfo ()
      createRankingList container config gameMode (ValueSome id) data
    |]

  | _ -> Array.empty


let create (container: Container) (state: MenuV2.State) =
  createBase()
  |> BoxUI.withChildren (
    state |> function
    | MenuV2.State.MainMenuState (_, state) ->
      createMainMenu container state

    | MenuV2.State.GameModeSelectState (WithContext state, _) ->
      createGamemodeSelect container state

    | MenuV2.State.SettingMenuState (state, _) ->
      createSetting container state

    | MenuV2.State.ControllerSelectState (WithContext(MenuV2.ControllerSelectToPlay state), _) ->
      [|
        createBackground container
        createCurrentModeMenu container container.TextMap.modes.controllerSelect
        yield!
          createControllerSelect container state
      |]

    | MenuV2.State.PauseState(state, _) ->
      createPause container state

    | MenuV2.State.GameResultState(state, _) ->
      createGameResult container state

    | _ -> Array.empty
  )
