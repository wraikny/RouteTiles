module RouteTiles.Core.Types.Menu
open RouteTiles.Core
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
    | Achievement
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
  SlideCount: int
  // Kind: int
}

[<RequireQualifiedAccess>]
type GameRankingState =
  | InputName of name:char[]
  | Waiting
  | Error of err:string
  | Success of int64 * SimpleRankingsServer.Data<GameResult>[]

[<RequireQualifiedAccess>]
type SettingMode =
  | InputtingName
  | InputName
  | Background
  | Enter

type SettingState = {
  mode: SettingMode
  modeCursor: int
  vertCursor: int
  name: char[]
  background: int
  prevConfig: Config
} with
  static member Init (config: Config) = {
    mode = SettingMode.InputName
    modeCursor = 0
    vertCursor = 0
    name = config.name |> function
      | ValueNone -> Array.empty
      | ValueSome s -> s.ToCharArray()
    background = Background.items |> Array.findIndex((=) config.background)
    prevConfig = config
  }

[<RequireQualifiedAccess>]
type State =
  | Menu
  | GameSetting of SoloGameMode * settingState:GameSettingState
  | Game of SoloGame.Mode * Controller
  | PauseGame of SoloGame.Mode * Controller * index:int
  | GameResult of SoloGame.Mode * GameResult * GameRankingState
  // | NextGame of SoloGame.Mode * Controller * index:int
  | Ranking of int * ((SoloGame.Mode * SimpleRankingsServer.Data<GameResult>[])[] voption)
  // | Achievement
  | Setting of SettingState
  | Erro of string * State
with
  member this.ControllerRefreshEnabled = this |> function
    | GameSetting _ -> true
    | _ -> false

  member this.IsStringInputMode = this |> function
    | GameResult (_, _, GameRankingState.InputName _) -> true
    | Setting(s) when s.mode = SettingMode.InputtingName -> true
    | _ -> false

type Model = {
  config: Config
  cursor: Mode
  state: State
}

let initModel config = {
  config = config
  cursor = Mode.SoloGame SoloGameMode.TimeAttack
  state = State.Menu
}

[<Struct; RequireQualifiedAccess>]
type StringInput = Input of char | Delete | Enter

[<RequireQualifiedAccess>]
type Msg =
  | MoveMode of Dir
  | Select
  | Back
  | RefreshController of Controller[]
  | Pause
  | FinishGame of SoloGame.Model * second:float32
  | RankingResult of Result<int64 * SimpleRankingsServer.Data<GameResult>[], string>
  | InputName of StringInput

let [<Literal>] UsernameMaxLength = 16

[<Struct; RequireQualifiedAccess>]
type SoloGameModeStrict =
  | TimeAttack2000
  | TimeAttack5000
  | TimeAttack10000
  | ScoreAttack180
  | ScoreAttack300
  | ScoreAttack600
#if DEBUG
  | DebugMode
#endif
with
  static member From(x) = x |> function
    | SoloGame.Mode.TimeAttack 2000 -> TimeAttack2000
    | SoloGame.Mode.TimeAttack 5000 -> TimeAttack5000
    | SoloGame.Mode.TimeAttack 10000 -> TimeAttack10000
    | SoloGame.Mode.ScoreAttack 180 -> ScoreAttack180
    | SoloGame.Mode.ScoreAttack 300 -> ScoreAttack300
    | SoloGame.Mode.ScoreAttack 600 -> ScoreAttack600
    | SoloGame.Mode.ScoreAttack 20 -> DebugMode
    | _ -> failwith "Unexpected SoloGame.Mode"


let timeAttackScores = [|
  2000
  5000
  10000
|]

let scoreAttackSecs = [|
  180
  300
  600
#if DEBUG
  20
#endif
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

let gameModes = [|
  for t in timeAttackScores -> SoloGame.Mode.TimeAttack t
  for s in scoreAttackSecs -> SoloGame.Mode.ScoreAttack s
|]

let gameModeToInt =
  let pairs = [|
    let mutable index = -1

    for t in timeAttackScores do
      index <- index + 1
      yield (SoloGame.Mode.TimeAttack t, index)

    index <- index + 1000

    for s in scoreAttackSecs do
      index <- index + 1
      yield (SoloGame.Mode.ScoreAttack s, index)
  |]

  dict pairs


let settingModes = [|
  SettingMode.InputName
  SettingMode.Background
  SettingMode.Enter
|]

// let nextGameSelect = [|
//   PauseSelect.Restart
//   PauseSelect.QuitGame
// |]

module Model =
  let inline mapConfig f model = { model with config = f model.config }