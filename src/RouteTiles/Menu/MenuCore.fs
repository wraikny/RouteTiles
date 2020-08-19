module RouteTiles.App.MenuCore
open RouteTiles.Core.Types

open Affogato

[<Struct; RequireQualifiedAccess>]
type SoloGameMode = TimeAttack | ScoreAttack

[<Struct; RequireQualifiedAccess>]
type Mode =
  | SoloGame of SoloGameMode
  | VS
  | Ranking
  | Achievement
  | Setting
with
  static member TimeAttack = SoloGame SoloGameMode.TimeAttack
  static member ScoreAttack = SoloGame SoloGameMode.ScoreAttack

  member this.IsEnabled = this |> function
    | VS -> false
    | _ -> true

let timeAttackScores = [|
  10000
  50000
  100000
|]

let scoreAttackSecs = [|
  60.0f * 3.0f
  60.0f * 5.0f
  60.0f * 10.0f
|]

// GameModel union
// Pause of GameModel
// ControllerSelect
[<Struct; RequireQualifiedAccess>]
type GameSettingMode =
  | ModeIndex
  | Controller
  | GameStart
with
  member this.ToInt = this |> function
    | ModeIndex -> 0
    | Controller -> 1
    | GameStart -> 2

type GameSettingState = {
  mode: GameSettingMode
  verticalCursor: int
  index: int
  controllerCursor: int
  selectedController: Controller
  controllers: Controller[]
} with
  static member Init(controllers) = {
    mode = GameSettingMode.ModeIndex
    verticalCursor = 0
    index = 0
    controllerCursor = 0
    selectedController = Controller.Keyboard
    controllers = controllers
  }

  member this.ControllerNames =
    this.controllers
    |> Array.map(function
      | Controller.Keyboard -> "Keyboard"
      | Controller.Joystick(_, name, _) -> name
    )

[<RequireQualifiedAccess>]
type State =
  | Menu
  | GameSetting of gameMode:SoloGameMode * timeAttack:GameSettingState
  | RankingTime of index:int
  | RankingScore of index:int
  | Achievement
  | Setting
with
  member this.ControllerRefreshEnabled = this |> function
    | GameSetting _ -> true
    | _ -> false


type Model = { cursor: Mode; state: State }

[<Struct; RequireQualifiedAccess>]
type Msg =
  | MoveMode of dir:Dir
  | Select
  | Back
  | RefreshController of Controller[]

let initModel = { cursor = Mode.SoloGame SoloGameMode.TimeAttack; state = State.Menu }

module Mode =
  let toVec mode =
    let (x, y) = mode |> function
      | Mode.SoloGame SoloGameMode.TimeAttack -> (0, 0)
      | Mode.SoloGame SoloGameMode.ScoreAttack -> (1, 0)
      | Mode.VS -> (2, 0)
      | Mode.Ranking -> (0, 1)
      | Mode.Achievement -> (1, 1)
      | Mode.Setting -> (2, 1)
    Vector2.init x y

  let fromVec (v: int Vector2) =
    let x = (v.x + 3) % 3
    let y = (v.y + 2) % 2

    (x, y) |> function
    | (0, 0) -> Mode.SoloGame SoloGameMode.TimeAttack
    | (1, 0) -> Mode.SoloGame SoloGameMode.ScoreAttack
    | (2, 0) -> Mode.VS
    | (0, 1) -> Mode.Ranking
    | (1, 1) -> Mode.Achievement
    | (2, 1) -> Mode.Setting
    | a -> failwithf "invalid input: %A" a

open EffFs

type CurrentControllers = CurrentControllers with
  static member Effect(_) = Eff.output<Controller[]>

[<Struct; RequireQualifiedAccess>]
type SoundKind =
  | Select
  | Move
  | Invalid

type SoundEffect = SoundEffect of SoundKind with
  static member Effect(_) = Eff.output<unit>

type GameStartEffect = GameStartEffect of SoloGame.Mode with
  static member Effect(_) = Eff.output<unit>


let inline moveSettingMode (isRight) (mode: GameSettingMode) = eff {
  match (mode, isRight) with
  | GameSettingMode.ModeIndex, true
  | GameSettingMode.GameStart, false ->
    do! SoundEffect SoundKind.Move
    return GameSettingMode.Controller
  
  | GameSettingMode.Controller, true ->
    do! SoundEffect SoundKind.Move
    return GameSettingMode.GameStart
  
  | GameSettingMode.Controller, false ->
    do! SoundEffect SoundKind.Move
    return GameSettingMode.ModeIndex
  
  | _ ->
    do! SoundEffect SoundKind.Invalid
    return mode
}

let inline updateGameSetting msg selectionCount gameMode (setting: GameSettingState) =
  eff {
    match msg, setting.mode with
    | Msg.Back, _ -> return setting

    | Msg.RefreshController controllers, _ ->
      return { setting with controllers = controllers }

    | Msg.Select, GameSettingMode.ModeIndex ->
      if setting.index = setting.verticalCursor then
        return setting
      else
        do! SoundEffect SoundKind.Select
        return { setting with index = setting.verticalCursor }

    | Msg.Select, GameSettingMode.Controller ->
      let targetController =
        setting.controllers
        |> Array.tryItem setting.controllerCursor
        |> Option.defaultValue Controller.Keyboard
      if setting.selectedController = targetController then
        return setting
      else
        do! SoundEffect SoundKind.Select
        return { setting with selectedController = targetController }

    // ゲームスタート
    | Msg.Select, GameSettingMode.GameStart ->
      do! SoundEffect SoundKind.Select
      do! GameStartEffect(gameMode |> function
        | SoloGameMode.TimeAttack -> SoloGame.Mode.TimeAttack(timeAttackScores.[setting.index])
        | SoloGameMode.ScoreAttack -> SoloGame.Mode.ScoreAttack(scoreAttackSecs.[setting.index])
      )
      return setting

    // モード切替
    | Msg.MoveMode Dir.Right, _ ->
        let! mode = setting.mode |> moveSettingMode true
        return { setting with mode = mode; verticalCursor = 0 }
    | Msg.MoveMode Dir.Left, _ ->
        let! mode = setting.mode |> moveSettingMode false
        return { setting with mode = mode; verticalCursor = 0 }

    // ゲームモード選択
    | Msg.MoveMode dir, GameSettingMode.ModeIndex ->
      let newCursor = setting.verticalCursor + (if dir = Dir.Up then -1 else +1)
      if newCursor < 0 || selectionCount <= newCursor then
        do! SoundEffect SoundKind.Invalid
        return setting
      else
        do! SoundEffect SoundKind.Move
        return { setting with verticalCursor = newCursor }

    // コントローラー選択
    | Msg.MoveMode dir, GameSettingMode.Controller ->
      let newCursor = setting.controllerCursor + (if dir = Dir.Up then -1 else +1)
      if newCursor < 0 || setting.controllers.Length <= newCursor then
        do! SoundEffect SoundKind.Invalid
        return setting
      else
        do! SoundEffect SoundKind.Move
        return { setting with controllerCursor = newCursor }

    | _, GameSettingMode.GameStart ->
      do! SoundEffect SoundKind.Invalid
      return setting
  }

let inline update msg model = eff {
  match msg, model with
  | Msg.Back, _ ->
    return { model with state = State.Menu }

  | Msg.MoveMode dir, { cursor = cursor; state = State.Menu } ->
    do! SoundEffect SoundKind.Move
    return
      { model with
          cursor =
            (Dir.toVector dir) + (Mode.toVec cursor)
            |> Mode.fromVec
      }

  | msg, { state = State.GameSetting(gameMode, setting) } ->
    let! newSetting = setting |> updateGameSetting msg timeAttackScores.Length gameMode
    return
      if setting = newSetting then
        model
      else
        { model with state = State.GameSetting(gameMode, newSetting) }

  | Msg.Select, { cursor = cursor; state = State.Menu } ->

    do! SoundEffect (if cursor.IsEnabled then SoundKind.Select else SoundKind.Invalid)

    match cursor with
    | Mode.SoloGame gameMode ->
      let! controllers = CurrentControllers
      return
        { model with
            state = State.GameSetting (gameMode, GameSettingState.Init controllers)
        }

    | Mode.Ranking ->
      return { model with state = State.RankingTime 0 }

    | Mode.Achievement ->
      return { model with state = State.Achievement }

    |  Mode.Setting ->
      return { model with state = State.Setting }

    | _ ->
      return model

  | Msg.RefreshController controllers, { state = state } ->
    return
      match state with
      | State.GameSetting(gameMode, setting) ->
        { model with state = State.GameSetting (gameMode, { setting with controllers = controllers })}
      | _ -> model

  | _ -> return model
}
