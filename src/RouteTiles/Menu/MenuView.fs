module RouteTiles.App.MenuView

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI
open Altseed2.BoxUI.Elements

open RouteTiles.Core.Types
open RouteTiles.App.BoxUIElements
open RouteTiles.App.MenuCore


let fontName() = Font.LoadDynamicFontStrict(Consts.ViewCommon.font, 60)
let fontDesc() = Font.LoadDynamicFontStrict(Consts.ViewCommon.font, 40)

let private modeButtons = [|
  Consts.Menu.timeAttackTexture, Mode.TimeAttack
  Consts.Menu.scoreAttackTexture, Mode.ScoreAttack
  Consts.Menu.questionTexture, Mode.VS
  Consts.Menu.rankingTexture, Mode.Ranking
  Consts.Menu.achievementTexture, Mode.Achievement
  Consts.Menu.settingTexture, Mode.Setting
|]

let private buttonIcon path = 
  let texture = Texture2D.LoadStrict path
  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.iconBackground)
  |> BoxUI.withChild(
    Sprite.Create(
      aspect = Aspect.Keep,
      zOrder = ZOrder.Menu.icon,
      texture = texture,
      color = Consts.Menu.iconColor
    )
    |> BoxUI.margin (LengthScale.RelativeMin, 0.1f)
    :> Element
  )

let private highlighten (movement: float32) (color: Color) (button: Rectangle) =
  let mutable selectedTime = 0.0f
  button.AddChild(
    Rectangle.Create(zOrder = ZOrder.Menu.iconSelected)
    |> BoxUI.onUpdate (fun (node: RectangleNode) ->
      let sinTime = MathF.Sin(selectedTime * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)
      if movement <> 0.0f then
        button.SetMargin(LengthScale.RelativeMin, movement * sinTime) |> ignore
      
      let a = (1.0f + sinTime) * 0.5f
      let (aMin, aMax) = Consts.Menu.cursorAlphaMinMax
      let alpha = (a * (aMax - aMin) + aMin) * 255.0f |> byte
      let color = Color (color.R, color.G, color.B, alpha)
      node.Color <- color
      selectedTime <- selectedTime + Engine.DeltaSecond)
    :> Element
  )

let private mainButtons (model: Model) =
  [|for (path, mode) in modeButtons ->
      let buttonIcon = buttonIcon path
      if mode = model.cursor then
        buttonIcon
        |> highlighten -0.05f (Consts.Menu.cursorColor)
      buttonIcon
  |]

let private mainMenuArea(children) =
  Empty.Create()
  |> BoxUI.withChildren [|
    Empty.Create()
    |> BoxUI.marginX (LengthScale.Relative, 0.1f)
    |> BoxUI.marginTop (LengthScale.Relative, 0.08f)
    |> BoxUI.withChildren children
  |]
  :> Element

let private mainMenu (model: Model) =
  mainMenuArea [|
    Grid.Create(Vector2F(1.0f, 1.0f) * 180.0f) :> Element
    |> BoxUI.withChildren (mainButtons model)
    // Rectangle.Create(zOrder = ZOrder.Menu.footer) :> Element
    // |> BoxUI.marginTop (LengthScale.Relative, 0.8f)
  |]

let textButton text =
  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.iconBackground)
  |> BoxUI.withChild (
    Text.Create(text = text, font = fontDesc(), color = Consts.Menu.textColor, zOrder = ZOrder.Menu.icon)
    |> BoxUI.alignCenter
  )

let settingHeader (items: string[]) (current: int) =
  Column.Create(ColumnDir.X)
  |> BoxUI.withChildren [|
    for (index, name) in items |> Seq.indexed do
      let elem =
        textButton name
        |> BoxUI.margin (LengthScale.RelativeMin, 0.05f)

      if index = current then
        highlighten 0.0f (Consts.Menu.cursorColor) elem

      elem
  |]
  :> Element

let verticalSelecter (items: string[]) (cursor: int) (current: int) =
  ItemList.Create(itemHeight = 40.0f, itemMargin = 10.0f)
  |> BoxUI.withChildren [|
    for (index, name) in items |> Seq.indexed do
      let elem = textButton name

      if index = current then
        highlighten 0.0f (Color(50uy, 50uy, 200uy)) elem

      if index = cursor then
        highlighten -0.05f (Consts.Menu.cursorColor) elem

      elem
  |]
  :> Element

let gameStart() =
  let elem = textButton "ゲームスタート"
  highlighten -0.02f (Consts.Menu.cursorColor) elem

  FixedSize.Create(Vector2F(300.0f, 150.0f))
  |> BoxUI.alignX Align.Center
  |> BoxUI.withChild elem
  :> Element

type Description = { name: string; desc: string }

let private sideBar (description: Description) =
  Rectangle.Create(color = Consts.Menu.elementBackground, zOrder = ZOrder.Menu.sideMenuBackground)
  |> BoxUI.withChild (
    ItemList.Create(itemMargin = 10.0f)
    |> BoxUI.marginXY (LengthScale.Relative, 0.05f, 0.06f)
    |> BoxUI.withChildren [|
      Text.Create(
        aspect = Aspect.Fixed,
        text = description.name,
        font = fontName(),
        zOrder = ZOrder.Menu.sideMenuText,
        color = Consts.Menu.textColor
      ) :> Element
      Text.Create(
        aspect = Aspect.Fixed,
        text = description.desc,
        font = fontDesc(),
        zOrder = ZOrder.Menu.sideMenuText,
        color = Consts.Menu.textColor
      ) :> Element
    |]
  )
  :> Element

let modeTexts = dict <| seq {
  ( Mode.TimeAttack
    , {
      name = "タイムアタック"
      desc = "目標スコアまでの時間を\n競うモードです。"
    })
  ( Mode.ScoreAttack
    , {
      name = "スコアアタック"
      desc = "制限時間内のスコアを\n競うモードです。"
    })
  ( Mode.VS
    , {
      name = "？？？"
      desc = "未実装の機能です。"
    })
  ( Mode.Ranking
    , {
      name = "ランキング"
      desc = "オンラインランキングを\nチェックしよう！"
    })
  ( Mode.Achievement
    , {
      name = "実績"
      desc = "開放した実績を確認します。"
    })
  ( Mode.Setting
    , {
      name = "設定"
      desc = "各種設定を行います。"
    })
}

let timeAttackSettingModeDescs = dict [|
  GameSettingMode.ModeIndex
  , { name = "目標スコア"; desc = "より短い時間で目標スコアを\n目指します。" }
  GameSettingMode.Controller
  , { name = "コントローラー"; desc = "使用するコントローラーを\n選択します。" }
  GameSettingMode.GameStart
  , { name = "ゲームスタート"; desc = "ゲームを開始します。" }
|]

let timeAttackSettingModeNames =
  [| for x in timeAttackSettingModeDescs -> x.Value.name |]

let scoreAttackSettingModeDescs = dict [|
  GameSettingMode.ModeIndex
  , { name = "制限時間"; desc = "時間内に多くのスコアを\n取るのが目標です。" }
  GameSettingMode.Controller
  , { name = "コントローラー"; desc = "使用するコントローラーを\n選択します。" }
  GameSettingMode.GameStart
  , { name = "ゲームスタート"; desc = "ゲームを開始します。" }
|]

let scoreAttackSettingModeNames =
  [| for x in scoreAttackSettingModeDescs -> x.Value.name |]

let timeAttackScoreNames = 
  timeAttackScores
  |> Array.map(sprintf "%d 点")

let scoreAttackSecNames =
  scoreAttackSecs
  |> Array.map(fun x ->
    sprintf "%d 分" (x / 60.0f |> int)
  )

let gameSettingVerticalSelecter modeNames (setting: GameSettingState) =
  setting.mode |> function
  | GameSettingMode.ModeIndex ->
    verticalSelecter modeNames setting.verticalCursor setting.index
  | GameSettingMode.Controller ->
    verticalSelecter setting.ControllerNames setting.controllerCursor 0
  | GameSettingMode.GameStart ->
    gameStart()
  |> BoxUI.marginTop (LengthScale.Relative, 0.1f)

let menu (model: Model) =
  Window.Create()
  |> BoxUI.withChild (
    let (item1, item2) =
      model.state |> function
      | State.Menu ->
        (mainMenu model),
        (sideBar <| modeTexts.[model.cursor])
      | State.GameSetting (gameMode, setting) ->
        let modeNames, selectionNames, descs = gameMode |> function
          | SoloGameMode.TimeAttack -> timeAttackSettingModeNames, timeAttackScoreNames, timeAttackSettingModeDescs
          | SoloGameMode.ScoreAttack -> scoreAttackSettingModeNames, scoreAttackSecNames, scoreAttackSettingModeDescs

        mainMenuArea [|
          split2 ColumnDir.Y 0.05f
            (settingHeader modeNames setting.mode.ToInt)
            (setting |> gameSettingVerticalSelecter selectionNames |> BoxUI.marginTop (LengthScale.Relative, 0.1f))
        |],
        (descs.[setting.mode]) |> sideBar

      | _ ->
        Empty.Create() :> Element,
        sideBar { name = ""; desc = "" }

    split2
      ColumnDir.X
      Consts.Menu.mainMenuRatio
      item1 item2
  )


let initialize (progress: unit -> int) =
  let texts =[|
    for x in modeTexts -> x.Value
    for x in timeAttackSettingModeDescs -> x.Value
    for x in scoreAttackSettingModeDescs -> x.Value
  |]

  let otherCharacters = [|
    for i in 'a'..'z' -> i
    for i in 'A'..'Z' -> i
    yield! "点分ゲームスタート"
  |]

  let progressSum =
    texts
    |> Seq.sumBy(fun x -> x.name.Length + x.desc.Length)
    |> (+) otherCharacters.Length
    |> (+) 2

  progressSum, async {
    let ctx = SynchronizationContext.Current
    do! Async.SwitchToThreadPool()

    let fontName = fontName()
    progress() |> ignore
    let fontDesc = fontDesc()
    progress() |> ignore

    do! Async.SwitchToContext(ctx)

    let Step = 10
    for x in texts do
      for c in x.name do
        fontName.GetGlyph(int c) |> ignore
        if progress() % Step = 0 then
          do! Async.Sleep 1

      for c in x.desc do
        if c <> '\n' then
          fontDesc.GetGlyph(int c) |> ignore
          if progress() % Step = 0 then
            do! Async.Sleep 1

    for c in otherCharacters do
      fontDesc.GetGlyph(int c) |> ignore
      if progress() % Step = 0 then
        do! Async.Sleep 1
  }