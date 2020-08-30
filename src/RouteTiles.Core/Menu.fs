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

let inline updateGameSetting msg selectionCount (setting: GameSettingState) =
  eff {
    match msg, setting.mode with
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

    // モード切替
    | Msg.MoveMode Dir.Right, _ ->
        let! mode = setting.mode |> moveSettingMode true
        return { setting with mode = mode; verticalCursor = 0 }
    | Msg.MoveMode Dir.Left, _ ->
        let! mode = setting.mode |> moveSettingMode false
        return { setting with mode = mode; verticalCursor = 0 }

    // 縦方向選択
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

    | _, GameSettingMode.GameStart ->
      do! SoundEffect.Invalid
      return setting

    | _ ->
      return setting
  }

let inline update msg model = eff {
  match msg, model with
  // Game
  | Msg.Pause, { state = State.Game (gameMode, controller) } ->
    do! GameControlEffect.Pause
    return { model with state = State.PauseGame (gameMode, controller, 0) }

  // ゲーム終了
  | Msg.FinishGame (soloGameModel, time), { state = State.Game (mode, _) } ->
    let result = {
      Name = ""
      Point = soloGameModel.board.point
      Time = time
      SlideCount = soloGameModel.board.slideCount
      // Kind = gameModeToInt.[mode]
    }

    // デフォルト名が設定されているかどうか
    let result =
      model.config.name |> function
      | ValueSome username when username <> "" ->
        { result with Name = username }
      | _ -> result

    // match model.config.name with
    // | ValueSome username when username <> "" ->
    //   let result = { result with Name = username }
    //   let param = {|
    //     mode = mode
    //     guid = model.config.guid
    //     result = result
    //     onSuccess = Ok >> Msg.RankingResult
    //     onError = Error >> Msg.RankingResult
    //   |}

    //   do! GameRankingEffect param
    //   return { model with state = State.GameResult(mode, result, GameRankingState.Waiting) }
    // | _ ->
    do! GameControlEffect.Pause
    return { model with state = State.GameResult(mode, result, GameRankingState.InputName <| result.Name.ToCharArray()) }
  
  | _, { state = State.Game _ } -> return model

  // ゲームリザルト
  | Msg.InputName stringInput, { state = State.GameResult(mode, result, GameRankingState.InputName name)} ->
    let setName n =
      { model with state = State.GameResult(mode, result, GameRankingState.InputName n)}

    match stringInput with
    | StringInput.Input c ->
      if name.Length >= UsernameMaxLength then
        do! SoundEffect.Invalid
        return model
      else
        return setName[| yield! name; yield c|]
    | StringInput.Delete ->
      if name |> Array.isEmpty then
        do! SoundEffect.Invalid
        return model
      else
        return setName name.[0..name.Length-2]
    | StringInput.Enter ->
      if name.Length < 1 then
        do! SoundEffect.Invalid
        return model
      else
        let result = { result with Name = new System.String(name) }
        let param = {|
          mode = mode
          guid = model.config.guid
          result = result
          onSuccess = Ok >> Msg.RankingResult
          onError = Error >> Msg.RankingResult
        |}

        do! GameRankingEffect param
        return { model with state = State.GameResult(mode, result, GameRankingState.Waiting) }

  // ランキング受け取り
  | Msg.RankingResult rRes, { state = State.GameResult(mode, res, GameRankingState.Waiting) } ->
    let rankingState = rRes |> function
      | Ok x -> GameRankingState.Success x
      | Error e -> GameRankingState.Error e

    return { model with state = State.GameResult(mode, res, rankingState)}

  | _, { state = State.GameResult(_, _, GameRankingState.Waiting)} ->
    return model

  // todo
  | Msg.Select, { state = State.GameResult(_, _, GameRankingState.Success _) }
  | Msg.Select, { state = State.GameResult(_, _, GameRankingState.Error _) }
  | Msg.Back, { state = State.GameResult(_, _, _) } ->
    do! GameControlEffect.Resume
    do! GameControlEffect.Quit
    do! SoundEffect.Select
    return { model with state = State.Menu }


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
      do! GameControlEffect.Resume

      match pauseSelects.[index] with
      // 再開
      | PauseSelect.Continue ->
        return { model with state = State.Game (gameMode, controller) }

      | PauseSelect.Restart ->
        do! GameControlEffect.Restart
        return { model with state = State.Game (gameMode, controller) }

      | PauseSelect.QuitGame ->
        do! GameControlEffect.Quit
        return { model with state = State.Menu }

    | _ ->
      return model

  // 設定
  // 名前入力中
  | msg, { state = State.Setting({ mode = SettingMode.InputtingName } as s) } ->
    match msg with
    | Msg.MoveMode _ -> return model
    | Msg.Back ->
      let name = model.config.name |> function
        | ValueNone -> Array.empty
        | ValueSome s -> s.ToCharArray()

      return
        { model with state = State.Setting({ s with mode = SettingMode.InputName; name = name }) }

    | Msg.InputName (stringInput) ->
      let setName n =
        { model with state = State.Setting({ s with name = n }) }

      match stringInput with
      | StringInput.Input c ->
        if s.name.Length >= UsernameMaxLength then
          do! SoundEffect.Invalid
          return model
        else
          return setName [| yield! s.name; yield c|]
      | StringInput.Delete ->
        if s.name |> Array.isEmpty then
          do! SoundEffect.Invalid
          return model
        else
          return setName s.name.[0..s.name.Length-2]
      | StringInput.Enter ->
        do! SoundEffect.Select

        let name = if s.name |> Array.isEmpty then ValueNone else ValueSome <| new System.String(s.name)

        return
          { model with state = State.Setting({ s with mode = SettingMode.InputName }) }
          |> Model.mapConfig(fun c -> { c with name = name })

    | _ -> return model

  | Msg.MoveMode dir, { state = State.Setting(s) } ->
    match dir with
    | Dir.Right | Dir.Left ->
      let newCursor = s.modeCursor + if dir = Dir.Right then +1 else -1
      if newCursor < 0 || settingModes.Length <= newCursor then
        do! SoundEffect.Invalid
        return model
      else
        let mode = settingModes.[newCursor]
        do! SoundEffect.Move
        return { model with state = State.Setting({ s with mode = mode; vertCursor = 0; modeCursor = newCursor }) }
    | Dir.Up | Dir.Down ->
      let mode = settingModes.[s.modeCursor]
      match mode with
      | SettingMode.InputtingName
      | SettingMode.InputName
      | SettingMode.Enter ->
        do! SoundEffect.Invalid
        return model
      | SettingMode.Background ->
        let newVert = s.vertCursor + if dir = Dir.Down then +1 else -1
        if newVert < 0 || Background.items.Length <= newVert then
          do! SoundEffect.Invalid
          return model
        else
          do! SoundEffect.Move
          return { model with state = State.Setting({ s with vertCursor = newVert})}

  | Msg.Select, { state = State.Setting(s) } ->
    let mode = settingModes.[s.modeCursor]
    
    match mode with
    | SettingMode.InputName ->
      do! SoundEffect.Select
      return
        { model with state = State.Setting({ s with mode = SettingMode.InputtingName }) }
    | SettingMode.Background ->
      return
        model
        |> Model.mapConfig(fun c -> { c with background = Background.items.[s.vertCursor] })

    | SettingMode.Enter ->
      // 設定保存
      do! SaveConfig model.config
      return { model with state = State.Menu }

    | SettingMode.InputtingName ->
      return model

  // 元のコンフィグに戻す。
  | Msg.Back, { state = State.Setting(s) } ->
    return { model with state = State.Menu; config = s.prevConfig }

  // 戻る
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
      | SoloGameMode.ScoreAttack -> SoloGame.Mode.ScoreAttack(scoreAttackSecs.[setting.index])
    

    do! GameStartEffect(mode, controller)
    return { model with state = State.Game (mode, controller) }

  | msg, { state = State.GameSetting(gameMode, setting) } ->
    let selectionCount = gameMode |> function
      | SoloGameMode.TimeAttack -> timeAttackScores.Length
      | SoloGameMode.ScoreAttack -> scoreAttackSecs.Length

    let! newSetting = setting |> updateGameSetting msg selectionCount
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
      // todo
      return { model with state = State.Ranking(0, ValueNone) }

    // todo
    // | Mode.Achievement ->
    //   return { model with state = State.Achievement }

    |  Mode.Setting ->
      return { model with state = State.Setting(SettingState.Init model.config) }

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
