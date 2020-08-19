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

let private mainButtons (model: Model) =
  [|for (path, mode) in modeButtons ->
      let buttonIcon = buttonIcon path
      if mode = model.cursor then
        let mutable selectedTime = 0.0f
        buttonIcon.AddChild(
          Rectangle.Create(zOrder = ZOrder.Menu.iconSelected)
          |> BoxUI.onUpdate (fun (node: RectangleNode) ->
            let sinTime = MathF.Sin(selectedTime * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)
            buttonIcon.SetMargin(LengthScale.RelativeMin, -0.05f * sinTime) |> ignore
            
            let a = (1.0f + sinTime) * 0.5f
            let (aMin, aMax) = Consts.Menu.selectedAlphaMinMax
            let alpha = (a * (aMax - aMin) + aMin) * 255.0f |> byte
            let color = Color (Consts.Menu.iconSelectedColor.R, Consts.Menu.iconSelectedColor.G, Consts.Menu.iconSelectedColor.B, alpha)
            node.Color <- color
            selectedTime <- selectedTime + Engine.DeltaSecond)
          :> Element
        )

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
    Grid.Create(180.0f) :> Element
    |> BoxUI.withChildren (mainButtons model)
    // Rectangle.Create(zOrder = ZOrder.Menu.footer) :> Element
    // |> BoxUI.marginTop (LengthScale.Relative, 0.8f)
  |]

let controllerSelect (controllers: Controller[]) (current: int) =
  mainMenuArea [|
    ItemList.Create(itemHeight = 40.0f)
    |> BoxUI.withChildren [|
      for c in controllers do
        let name = c |> function
          | Controller.Keyboard -> "Keyboard"
          | Controller.Joystick (name, _) -> name
        
        Rectangle.Create(color = Consts.Menu.elementBackground)
        |> BoxUI.withChild (
          Text.Create(text = name, font = fontDesc())
        )
    |]
  |]

type Description = { name: string; desc: string }

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

let menu (model: Model) =
  Window.Create()
  |> BoxUI.withChild (
    let (item1, item2) =
      if model.state = State.Menu then
        (mainMenu model),
        (sideBar <| modeTexts.[model.cursor])
      else
        (Empty.Create() :> Element),
        (sideBar <| modeTexts.[model.cursor])
    
    split2
      ColumnDir.X
      Consts.Menu.mainMenuRatio
      item1 item2
  )


let initialize (progress: unit -> int) =
  let texts = modeTexts |> Seq.skip 1 |> Seq.map(fun x -> x.Value) |> Seq.toArray

  let progressSum =
    texts
    |> Seq.sumBy(fun x -> x.name.Length + x.desc.Length)
    |> (+) 2

  progressSum, async {
    let ctx = SynchronizationContext.Current
    do! Async.SwitchToThreadPool()

    let fontName = fontName()
    progress() |> ignore
    let fontDesc = fontDesc()
    progress() |> ignore

    do! Async.SwitchToContext(ctx)

    let Step = 5
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
  }