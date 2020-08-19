module RouteTiles.App.MenuCore
open RouteTiles.Core.Types

open Affogato

[<Struct; RequireQualifiedAccess>]
type Mode =
  | TimeAttack
  | ScoreAttack
  | VS
  | Ranking
  | Achievement
  | Setting
with
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

type GameSettingState = {
  mode: GameSettingMode
  verticalCursor: int
  index: int
  controller: int
  controllers: Controller[]
} with
  static member Init(controllers) = {
    mode = GameSettingMode.ModeIndex
    verticalCursor = 0
    index = 0
    controller = 0
    controllers = controllers
  }

[<RequireQualifiedAccess>]
type State =
  | Menu
  | TimeAttackSetting of timeAttack:GameSettingState
  | ScoreAttackSetting of scoreAttack:GameSettingState
  | RankingTime of index:int
  | RankingScore of index:int
  | Achievement
  | Setting
with
  member this.ControllerRefreshEnabled = this |> function
    | TimeAttackSetting _  | ScoreAttackSetting _ -> true
    | _ -> false


type Model = { cursor: Mode; state: State }

[<Struct; RequireQualifiedAccess>]
type Msg =
  | MoveMode of dir:Dir
  | Select
  | Back
  | RefreshController of Controller[]

let initModel = { cursor = Mode.TimeAttack; state = State.Menu }

module Mode =
  let toVec mode =
    let (x, y) = mode |> function
      | Mode.TimeAttack -> (0, 0)
      | Mode.ScoreAttack -> (1, 0)
      | Mode.VS -> (2, 0)
      | Mode.Ranking -> (0, 1)
      | Mode.Achievement -> (1, 1)
      | Mode.Setting -> (2, 1)
    Vector2.init x y

  let fromVec (v: int Vector2) =
    let x = (v.x + 3) % 3
    let y = (v.y + 2) % 2

    (x, y) |> function
    | (0, 0) -> Mode.TimeAttack
    | (1, 0) -> Mode.ScoreAttack
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


let inline moveSettingMode (isRight) (mode: GameSettingMode) = eff {
  match (mode, isRight) with
  | GameSettingMode.ModeIndex, true
  | GameSettingMode.GameStart, false ->
    do! SoundEffect SoundKind.Move
    return GameSettingMode.Controller
  
  | GameSettingMode.Controller, true ->
    do! SoundEffect SoundKind.Move
    return GameSettingMode.ModeIndex
  
  | GameSettingMode.Controller, false ->
    do! SoundEffect SoundKind.Move
    return GameSettingMode.GameStart
  
  | _ ->
    do! SoundEffect SoundKind.Invalid
    return mode
}

let inline updateTimeAttackSetting msg (setting: GameSettingState) =
  eff {
    match msg with
    | Msg.MoveMode dir ->
      match dir with
      | Dir.Right | Dir.Left ->
        let! mode = setting.mode |> moveSettingMode (dir = Dir.Right)
        return { setting with mode = mode }
      | _ ->
        return setting
    | _ ->
      return setting
  }

let inline updateScoreAttackSetting msg (setting: GameSettingState) =
  eff {
    match msg with
    | Msg.MoveMode dir ->
      match dir with
      | Dir.Right | Dir.Left ->
        let! mode = setting.mode |> moveSettingMode (dir = Dir.Right)
        return { setting with mode = mode }
      | _ ->
        return setting
    | _ ->
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

  | msg, { state = State.TimeAttackSetting setting } ->
    let! newSetting = updateTimeAttackSetting msg setting
    return
      if setting = newSetting then
        model
      else
        { model with state = State.TimeAttackSetting newSetting }
  | msg, { state = State.ScoreAttackSetting setting } ->
    let! newSetting = updateScoreAttackSetting msg setting
    return
      if setting = newSetting then
        model
      else
        { model with state = State.ScoreAttackSetting newSetting }

  | Msg.Select, { cursor = cursor; state = State.Menu } ->

    do! SoundEffect (if cursor.IsEnabled then SoundKind.Select else SoundKind.Invalid)

    match cursor with
    | Mode.TimeAttack ->
      let! controllers = CurrentControllers
      return
        { model with
            state = State.TimeAttackSetting (GameSettingState.Init controllers)
        }

    | Mode.ScoreAttack ->
      let! controllers = CurrentControllers
      return
        { model with
            state = State.ScoreAttackSetting (GameSettingState.Init controllers)
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
      | State.TimeAttackSetting setting ->
        { model with state = State.TimeAttackSetting { setting with controllers = controllers }}
      | State.ScoreAttackSetting setting ->
        { model with state = State.ScoreAttackSetting { setting with controllers = controllers }}
      | _ -> model

  | _ -> return model
}
