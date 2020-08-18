namespace RouteTiles.App

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI

open MenuCore

module MenuParams =
  let mainMenuRatio = 10.0f / 16.0f

  [<Literal>]
  let selectedTimePeriod = 2.0f

  module Color =
    let debugColor = Nullable <| Color(255uy, 0uy, 0uy, 100uy)
    let backgroundColor = Nullable <| Color(50uy, 50uy, 50uy)
    let sideBarColor = Nullable <| Color(150uy, 150uy, 150uy, 150uy)

    let iconBack = Nullable <| Color(240uy, 240uy, 240uy)
    let iconColor = Nullable <| Color(50uy, 50uy, 50uy, 255uy)
    let iconSelected = Color(255uy, 255uy, 0uy, 0uy)

    let text = Nullable <| Color(0uy, 0uy, 0uy)

  module Texture =
    let timeAttack = @"menu/stopwatch.png"
    let scoreAttack = @"menu/hourglass.png"
    let question = @"menu/question.png"
    let ranking = @"menu/crown.png"
    let achievement = @"menu/trophy.png"
    let setting = @"menu/gear.png"

    let textures = [|
      Mode.TimeAttack, timeAttack
      Mode.ScoreAttack, scoreAttack
      Mode.VS, question
      Mode.Ranking, ranking
      Mode.Achievement, achievement
      Mode.Setting, setting
    |]

    let initialize (progress: int -> unit) =
      textures.Length, async {
        let ctx = SynchronizationContext.Current
        do! Async.SwitchToThreadPool()

        let mutable count = 0
        for (_, path) in textures do
          Texture2D.Load(path) |> ignore
          count <- count + 1
          progress(count)
        
        do! Async.SwitchToContext(ctx)
      }

  module ZOrder =
    let offset = (|||) (100 <<< 16)
    let background = offset 0

    let footer = offset 10
    let iconBackground = offset 20
    let icon = offset 21
    let iconSelected = offset 22

    let sideMenuBackground = offset 31
    let sideMenuText = offset 32

module MenuElement =
  open Altseed2.BoxUI.Elements
  open Altseed2.BoxUI
  open RouteTiles.App.BoxUIElements
  open MenuParams
  open MenuCore

  let private modeButtons = [|
    Texture.timeAttack, Mode.TimeAttack
    Texture.scoreAttack, Mode.ScoreAttack
    Texture.question, Mode.VS
    Texture.ranking, Mode.Ranking
    Texture.achievement, Mode.Achievement
    Texture.setting, Mode.Setting
  |]

  let private mainButtons (model: Model) =
    [|for (path, mode) in modeButtons ->
        let texture = Texture2D.LoadStrict path

        let buttonIcon = 
          Rectangle.Create(color = Color.iconBack, zOrder = ZOrder.iconBackground)
          |> BoxUI.withChild(
            Sprite.Create(
              aspect = Aspect.Keep,
              zOrder = ZOrder.icon,
              texture = texture,
              color = Color.iconColor
            )
            |> BoxUI.margin (LengthScale.RelativeMin, 0.1f)
            :> Element
          )

        if mode = model.cursor then
          let mutable selectedTime = 0.0f
          buttonIcon.AddChild(
            Rectangle.Create(zOrder = ZOrder.iconSelected)
            |> BoxUI.onUpdate (fun (node: RectangleNode) ->
              let sinTime = MathF.Sin(selectedTime * 2.0f * MathF.PI / selectedTimePeriod)
              buttonIcon.SetMargin(LengthScale.RelativeMin, -0.05f * sinTime) |> ignore
              
              let a = (1.0f + sinTime) * 0.5f
              let alpha = (a * 0.4f + 0.2f) * 255.0f |> byte
              let color = Color (Color.iconSelected.R, Color.iconSelected.G, Color.iconSelected.B, alpha)
              node.Color <- color
              selectedTime <- selectedTime + Engine.DeltaSecond)
            :> Element
          )

        buttonIcon
    |]

  let private mainMenu (model: Model) =
    Empty.Create()
    |> BoxUI.marginRight (LengthScale.Relative, 1.0f - mainMenuRatio)
    |> BoxUI.withChildren [|
      Empty.Create()
      |> BoxUI.marginX (LengthScale.Relative, 0.1f)
      |> BoxUI.marginTop (LengthScale.Relative, 0.08f)
      |> BoxUI.withChildren [|
        Grid.Create(180.0f) :> Element
        |> BoxUI.withChildren (mainButtons model)
        // Rectangle.Create(zOrder = ZOrder.footer) :> Element
        // |> BoxUI.marginTop (LengthScale.Relative, 0.8f)
      |]
    |]
    :> Element

  let modeTexts = dict <| seq {
    ( Mode.TimeAttack
      , {|
        name = "タイムアタック"
        desc = "目標スコアまでの時間を\n競うモードです。"
      |})
    ( Mode.ScoreAttack
      , {|
        name = "スコアアタック"
        desc = "制限時間内のスコアを\n競うモードです。"
      |})
    ( Mode.VS
      , {|
        name = "？？？"
        desc = "未実装の機能です。"
      |})
    ( Mode.Ranking
      , {|
        name = "ランキング"
        desc = "オンラインランキングを\nチェックしよう！"
      |})
    ( Mode.Achievement
      , {|
        name = "実績"
        desc = "開放した実績を確認します。"
      |})
    ( Mode.Setting
      , {|
        name = "設定"
        desc = "各種設定を行います。"
      |})
  }

  let fontName() = Font.LoadDynamicFontStrict(Consts.ViewCommon.font, 60)
  let fontDesc() = Font.LoadDynamicFontStrict(Consts.ViewCommon.font, 40)

  let private sideBar (model: Model) =

    let modeText = modeTexts.[model.cursor]

    Rectangle.Create(color = Color.sideBarColor, zOrder = ZOrder.sideMenuBackground)
    |> BoxUI.marginLeft (LengthScale.Relative, mainMenuRatio)
    |> BoxUI.withChild (
      ItemList.Create(itemMargin = 10.0f)
      |> BoxUI.marginXY (LengthScale.Relative, 0.05f, 0.06f)
      |> BoxUI.withChildren [|
        Text.Create(
          aspect = Aspect.Fixed,
          text = modeText.name,
          font = fontName(),
          zOrder = ZOrder.sideMenuText,
          color = Color.text
        ) :> Element
        Text.Create(
          aspect = Aspect.Fixed,
          text = modeText.desc,
          font = fontDesc(),
          zOrder = ZOrder.sideMenuText,
          color = Color.text
        ) :> Element
      |]
    )
    :> Element

  let menu (model: Model) =
    Window.Create()
    |> BoxUI.withChildren [|
      Rectangle.Create(color = Color.backgroundColor, zOrder = ZOrder.background) :> Element
      mainMenu model
      sideBar model
    |]

  [<Literal>]
  let private Step = 3

  let initialize (progress: int -> unit) =

    let texts = modeTexts |> Seq.skip 1 |> Seq.map(fun x -> x.Value) |> Seq.toArray

    let progressSum =
      texts
      |> Seq.sumBy(fun x -> x.name.Length + x.desc.Length)
      |> (+) 2

    progressSum, async {
      let ctx = SynchronizationContext.Current
      do! Async.SwitchToThreadPool()

      let fontName = fontName()
      progress(1)
      let fontDesc = fontDesc()
      progress(2)

      do! Async.SwitchToContext(ctx)

      let mutable count = 0
      for x in texts do
        for c in x.name do
          fontName.GetGlyph(int c) |> ignore
          count <- count + 1
          progress(count + 2)
          if count % Step = 0 then
            do! Async.Sleep 1

        for c in x.desc do
          fontDesc.GetGlyph(int c) |> ignore
          count <- count + 1
          progress(count + 2)
          if count % Step = 0 then
            do! Async.Sleep 1
    }

open RouteTiles.Core.Types.Common

module MenuInput =
  open MenuCore

  let keyboard =
    [|
      for (key, _, _, dir) in InputControl.dirPairs -> [|key, ButtonState.Push|], Msg.MoveMode dir
    |]

  let joystick = [|
    for (_, btnL, _, dir) in InputControl.dirPairs -> (btnL, ButtonState.Push), Msg.MoveMode dir
  |]

open RouteTiles.Core.Utils

type Menu() =
  inherit Node()

  let updater = Updater<MenuCore.Model, MenuCore.Msg>()

  let coroutine = CoroutineNode()
  let uiRoot = BoxUIRootNode()

  do
    base.AddChildNode coroutine
    base.AddChildNode uiRoot

    updater.Subscribe(fun model ->
      uiRoot.ClearElement()
      uiRoot.SetElement <| MenuElement.menu model
    ) |> ignore

  let getKeyboardInput = InputControl.getKeyboardInput MenuInput.keyboard
  let getJoystickInput = InputControl.getJoystickInput MenuInput.joystick

  override __.OnUpdate() =
    getKeyboardInput ()
    |> Option.alt(fun () ->
      let count = Engine.Joystick.ConnectedJoystickCount
      seq {
        for i in 0..count-1 do
          let info = Engine.Joystick.GetJoystickInfo(i)
          if info.IsGamepad then
            match getJoystickInput i with
            | Some x -> yield x
            | _ -> ()
      }
      |> Seq.tryHead
    )
    |> Option.iter (updater.Dispatch >> ignore)

  override __.OnAdded() =
    updater.Init(MenuCore.initModel, MenuCore.update) |> ignore
