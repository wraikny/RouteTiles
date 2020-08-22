module RouteTiles.Core.Types.Menu
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

type GameResult = {
  Name: string
  Time: float32
  Point: int
}

[<RequireQualifiedAccess>]
type GameRankingState =
  | InputName of name:char[]
  | Waiting
  | Error of err:string
  | Success of int64 * SimpleRankingsServer.Data<GameResult>[]

[<RequireQualifiedAccess>]
type State =
  | Menu
  | GameSetting of SoloGameMode * settingState:GameSettingState
  | Game of SoloGame.Mode * Controller
  | PauseGame of SoloGame.Mode * Controller * index:int
  | GameResult of SoloGame.Mode * GameResult * GameRankingState
  // | NextGame of SoloGame.Mode * Controller * index:int
  | RankingTime of index:int
  | RankingScore of index:int
  | Achievement
  | Setting
  | Erro of string * State
with
  member this.ControllerRefreshEnabled = this |> function
    | GameSetting _ -> true
    | _ -> false


type Model = { cursor: Mode; state: State }

let initModel = { cursor = Mode.SoloGame SoloGameMode.TimeAttack; state = State.Menu }

[<Struct; RequireQualifiedAccess>]
type StringInput = Input of char | Delete | Enter

[<RequireQualifiedAccess>]
type Msg =
  | MoveMode of Dir
  | Select
  | Back
  | RefreshController of Controller[]
  | Pause
  | FinishGame of point:int * second:float32
  | RankingResult of Result<int64 * SimpleRankingsServer.Data<GameResult>[], string>
  | InputName of StringInput

let timeAttackScores = [|
  2000
  5000
  10000
|]

let scoreAttackSecs = [|
  60 * 3
  60 * 5
  60 * 10
|]

[<Struct; RequireQualifiedAccess>]
type PauseSelect =
  | Continue
  | Restart
  | QuitGame

let pauseSelects = [|
  PauseSelect.Continue
  PauseSelect.Restart
  PauseSelect.QuitGame
|]

// let nextGameSelect = [|
//   PauseSelect.Restart
//   PauseSelect.QuitGame
// |]
