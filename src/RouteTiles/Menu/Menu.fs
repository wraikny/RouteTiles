namespace RouteTiles.App

open System
open System.Threading
open Altseed2
open Altseed2.BoxUI

open MenuCore


module MenuElement =
  open Altseed2.BoxUI.Elements
  open Altseed2.BoxUI
  open RouteTiles.App.BoxUIElements
  open MenuCore

  let private modeButtons = [|
    Consts.Menu.timeAttackTexture, Mode.TimeAttack
    Consts.Menu.scoreAttackTexture, Mode.ScoreAttack
    Consts.Menu.questionTexture, Mode.VS
    Consts.Menu.rankingTexture, Mode.Ranking
    Consts.Menu.achievementTexture, Mode.Achievement
    Consts.Menu.settingTexture, Mode.Setting
  |]

  let private mainButtons (model: Model) =
    [|for (path, mode) in modeButtons ->
        let texture = Texture2D.LoadStrict path

        let buttonIcon = 
          Rectangle.Create(color = Consts.Menu.iconBackColor, zOrder = ZOrder.Menu.iconBackground)
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

        if mode = model.cursor then
          let mutable selectedTime = 0.0f
          buttonIcon.AddChild(
            Rectangle.Create(zOrder = ZOrder.Menu.iconSelected)
            |> BoxUI.onUpdate (fun (node: RectangleNode) ->
              let sinTime = MathF.Sin(selectedTime * 2.0f * MathF.PI / Consts.Menu.selectedTimePeriod)
              buttonIcon.SetMargin(LengthScale.RelativeMin, -0.05f * sinTime) |> ignore
              
              let a = (1.0f + sinTime) * 0.5f
              let alpha = (a * 0.4f + 0.2f) * 255.0f |> byte
              let color = Color (Consts.Menu.iconSelectedColor.R, Consts.Menu.iconSelectedColor.G, Consts.Menu.iconSelectedColor.B, alpha)
              node.Color <- color
              selectedTime <- selectedTime + Engine.DeltaSecond)
            :> Element
          )

        buttonIcon
    |]

  let private mainMenuArea(children) =
    Empty.Create()
    |> BoxUI.marginRight (LengthScale.Relative, 1.0f - Consts.Menu.mainMenuRatio)
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

  let fontName() = Font.LoadDynamicFontStrict(Consts.ViewCommon.font, 60)
  let fontDesc() = Font.LoadDynamicFontStrict(Consts.ViewCommon.font, 40)

  let private sideBar (description: Description) =
    Rectangle.Create(color = Consts.Menu.sideBarColor, zOrder = ZOrder.Menu.sideMenuBackground)
    |> BoxUI.marginLeft (LengthScale.Relative, Consts.Menu.mainMenuRatio)
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
    let background = Rectangle.Create(color = Consts.Menu.backgroundColor, zOrder = ZOrder.Menu.background) :> Element

    Window.Create()
    |> BoxUI.withChildren (
      if model.state = State.Menu then
        [|
          background
          mainMenu model
          sideBar <| modeTexts.[model.cursor]
        |]
      else
        [|
          background
          sideBar <| modeTexts.[model.cursor]
        |]
    )


  let initialize (progress: int -> unit) =
    let progress =
      let mutable count = 0
      fun () ->
        progress count
        count <- count + 1
        count

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
          fontDesc.GetGlyph(int c) |> ignore
          if progress() % Step = 0 then
            do! Async.Sleep 1
    }

open RouteTiles.Core.Types.Common

module MenuInput =
  open MenuCore

  let keyboard =
    [|
      for (key, _, _, dir) in InputControl.dirPairs -> [|key, ButtonState.Push|], Msg.MoveMode dir
      yield [|Key.Space, ButtonState.Push|], Msg.Select
      yield [|Key.Enter, ButtonState.Push|], Msg.Select
      yield [|Key.Escape, ButtonState.Push|], Msg.Back
      yield [|Key.Backspace, ButtonState.Push|], Msg.Back
    |]

  let joystick = [|
    for (_, btnL, _, dir) in InputControl.dirPairs -> (btnL, ButtonState.Push), Msg.MoveMode dir
    yield (JoystickButton.RightRight, ButtonState.Push), Msg.Select
    yield (JoystickButton.RightDown, ButtonState.Push), Msg.Back
    yield (JoystickButton.Guide, ButtonState.Push), Msg.Back
  |]

open RouteTiles.Core.Utils

open EffFs

type MenuHandler = MenuHandler with
  static member inline Handle(x) = x

  static member inline Handle(SelectSoundEffect t, k) =
    k()

  static member inline Handle(CurrentControllers, k) =
    [|
      yield Controller.Keyboard
      for i in 0..15 do
        let info = Engine.Joystick.GetJoystickInfo i
        if info <> null && info.IsGamepad then
          yield Controller.Joystick(info.GamepadName, i)
    |] |> k

type Menu() =
  inherit Node()

  let mutable prevModel = ValueNone
  let updater = Updater<MenuCore.Model, MenuCore.Msg>()

  let coroutine = CoroutineNode()
  let uiRoot = BoxUIRootNode()

  do
    base.AddChildNode coroutine
    base.AddChildNode uiRoot

    updater.Subscribe(fun model ->
      prevModel
      |> ValueOption.map(fun x -> Object.ReferenceEquals(x, model))
      |> ValueOption.defaultValue false
      |> function
      | true -> ()
      | false ->
        prevModel <- ValueSome model
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
    |> Option.iter (fun msg ->
      updater.Dispatch msg
      |> ignore
    )

  override __.OnAdded() =
    prevModel <-
      ( initModel,
        fun msg model ->
          printfn "Msg: %A" msg
          MenuCore.update msg model
          |> Eff.handle MenuHandler
      )
      |> updater.Init
      |> ValueSome
