module RouteTiles.Core.Menu

open EffFs

open RouteTiles.Core.Types
open RouteTiles.Core.Types.Menu
open RouteTiles.Core.Effects

let inline moveSettingMode (isRight) (mode: GameSettingMode) = eff {
  match (mode, isRight) with
  | GameSettingMode.ModeIndex, true
  | GameSettingMode.GameStart, false ->
    do! SoundEffect.Move
    return GameSettingMode.Controller
  
  | GameSettingMode.Controller, true ->
    do! SoundEffect.Move
    return GameSettingMode.GameStart
  
  | GameSettingMode.Controller, false ->
    do! SoundEffect.Move
    return GameSettingMode.ModeIndex
  
  | _ ->
    do! SoundEffect.Invalid
    return mode
}

let inline updateGameSetting msg selectionCount gameMode (setting: GameSettingState) =
  eff {
    match msg, setting.mode with
    | Msg.Pause, _
    | Msg.Back, _ ->
      return setting

    | Msg.RefreshController controllers, _ ->
      return { setting with controllers = controllers }

    | Msg.Select, GameSettingMode.ModeIndex ->
      if setting.index = setting.verticalCursor then
        return setting
      else
        do! SoundEffect.Select
        return { setting with index = setting.verticalCursor }

    | Msg.Select, GameSettingMode.Controller ->
      let targetController =
        setting.controllers
        |> Array.tryItem setting.controllerCursor
        |> Option.defaultValue Controller.Keyboard
      if setting.selectedController = targetController then
        return setting
      else
        do! SoundEffect.Select
        return { setting with selectedController = targetController }

    // ゲームスタート
    | Msg.Select, GameSettingMode.GameStart ->
      // 呼び出し元で拾う
      return setting
    | _, GameSettingMode.GameStart ->
      do! SoundEffect.Invalid
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
        do! SoundEffect.Invalid
        return setting
      else
        do! SoundEffect.Move
        return { setting with verticalCursor = newCursor }

    // コントローラー選択
    | Msg.MoveMode dir, GameSettingMode.Controller ->
      let newCursor = setting.controllerCursor + (if dir = Dir.Up then -1 else +1)
      if newCursor < 0 || setting.controllers.Length <= newCursor then
        do! SoundEffect.Invalid
        return setting
      else
        do! SoundEffect.Move
        return { setting with controllerCursor = newCursor }
  }

let inline update msg model = eff {
  match msg, model with
  // Game
  | Msg.Pause, { state = State.Game (gameMode, controller) } ->
    return { model with state = State.PauseGame (gameMode, controller, 0) }
  | _, { state = State.Game _ } -> return model

  // Pause
  | msg, { state = State.PauseGame (gameMode, controller, index)} ->
    match msg with

    | Msg.MoveMode Dir.Left
    | Msg.MoveMode Dir.Right -> return model

    | Msg.MoveMode dir ->
      let newIndex = index + if dir = Dir.Up then -1 else +1
      if newIndex < 0 || pauseSelects.Length <= newIndex then
        do! SoundEffect.Invalid
        return model
      else
        do! SoundEffect.Move
        return { model with state = State.PauseGame(gameMode, controller, newIndex) }

    // 再開
    | Msg.Back ->
      do! GameControlEffect.Resume
      return { model with state = State.Game (gameMode, controller) }
    | Msg.Select ->
      match pauseSelects.[index] with
      // 再開
      | PauseSelect.Continue ->
        do! GameControlEffect.Resume
        return { model with state = State.Game (gameMode, controller) }

      | PauseSelect.Restart ->
        do! GameControlEffect.Restart
        return { model with state = State.Game (gameMode, controller) }

      | PauseSelect.QuitGame ->
        do! GameControlEffect.Quit
        return { model with state = State.Menu }

    | _ ->
      return model

  | Msg.Back, _ ->
    return { model with state = State.Menu }

  | Msg.MoveMode dir, { cursor = cursor; state = State.Menu } ->
    do! SoundEffect.Move
    return
      { model with
          cursor =
            (Dir.toVector dir) + (Mode.toVec cursor)
            |> Mode.fromVec
      }

  // ゲームスタート
  | Msg.Select,
    { state = State.GameSetting (gameMode, setting) } when setting.mode = GameSettingMode.GameStart ->
    let controller = setting.selectedController
    let mode = gameMode |> function
      | SoloGameMode.TimeAttack -> SoloGame.Mode.TimeAttack(timeAttackScores.[setting.index])
      | SoloGameMode.ScoreAttack -> SoloGame.Mode.ScoreAttack(float32 scoreAttackSecs.[setting.index])
    

    do! GameStartEffect(mode, controller)
    return { model with state = State.Game (mode, controller) }

  | msg, { state = State.GameSetting(gameMode, setting) } ->
    let selectionCount = gameMode |> function
      | SoloGameMode.TimeAttack -> timeAttackScores.Length
      | SoloGameMode.ScoreAttack -> scoreAttackSecs.Length

    let! newSetting = setting |> updateGameSetting msg selectionCount gameMode
    return
      if setting = newSetting then
        model
      else
        { model with state = State.GameSetting(gameMode, newSetting) }

  | Msg.Select, { cursor = cursor; state = State.Menu } ->

    if cursor.IsEnabled then
      do! SoundEffect.Select
    else
      do! SoundEffect.Invalid

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
