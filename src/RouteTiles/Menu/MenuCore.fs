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

type TimeAttackSettingState = {
  scoreIndex: int
  controller: Controller
  controllers: Controller[]
}

type ScoreAttackSettingState = {
  secIndex: int
  controller: Controller
  controllers: Controller[]
}

[<RequireQualifiedAccess>]
type State =
  | Menu
  | TimeAttackSetting of timeAttack:TimeAttackSettingState
  | ScoreAttackSetting of scoreAttack:ScoreAttackSettingState
  | RankingTime of index:int
  | RankingScore of index:int
  | Achievement
  | Setting


type Model = { cursor: Mode; state: State }

[<Struct; RequireQualifiedAccess>]
type Msg =
  | MoveMode of dir:Dir
  | Select
  | Back

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

type SelectSoundEffect = SelectSoundEffect of bool with
  static member Effect(_) = Eff.output<unit>

let inline update msg model = eff {
  match msg, model with
  | Msg.Back, _ ->
    return { model with state = State.Menu }

  | Msg.MoveMode dir, { cursor = cursor; state = State.Menu } ->
    return
      { model with
          cursor =
            (Dir.toVector dir) + (Mode.toVec cursor)
            |> Mode.fromVec
      }

  | Msg.Select, { cursor = cursor; state = State.Menu } ->

    do! SelectSoundEffect (cursor.IsEnabled)

    match cursor with
    | Mode.TimeAttack ->
      let! controllers = CurrentControllers
      return
        { model with
            state = State.TimeAttackSetting { scoreIndex = 0; controller = Controller.Keyboard; controllers = controllers }
        }

    | Mode.ScoreAttack ->
      let! controllers = CurrentControllers
      return
        { model with
            state = State.ScoreAttackSetting { secIndex = 0; controller = Controller.Keyboard; controllers = controllers }
        }

    | Mode.Ranking ->
      return { model with state = State.RankingTime 0 }

    | Mode.Achievement ->
      return { model with state = State.Achievement }

    |  Mode.Setting ->
      return { model with state = State.Setting }

    | _ ->
      return model

  | _ -> return model
}
