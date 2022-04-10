namespace RouteTiles.App.Menu

open RouteTiles.App
open RouteTiles.Common
open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Menu.SubMenu
open RouteTiles.Menu.Types
open RouteTiles.Menu

open Altseed2

type internal Container (textMap: TextMap.TextMap) =
  let mutable progressCount = 0

  let makeLoadingTarget a =
    progressCount <- progressCount + 1
    a

  let backgroundTexture = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/background_dark.png"))
  let maskTexture = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/background_mask.png"))
  let titleTexture = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/title.png"))
  let buttonBackground = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/button-metalic-dark-highlight-320x80.png"))
  let inputBackground = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/input_background.png"))
  let inputFrame = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/input_frame.png"))
  let gameInfoFrame = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/game_info_frame.png"))
  let rankingFrame = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/ranking_frame.png"))
  let controllerBackground = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/controller_background.png"))
  let selectionArrow = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/selection_more.png"))

  let howToKeyboardShift = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howto_keyboard_shift.png"))
  let howToKeyboardSeparate = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howto_keyboard_separate.png"))
  let howToJoystick = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howto_joystick.png"))
  let howToSlide = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howtoplay_slide.png"))
  let howToRoute = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howtoplay_route.png"))
  let howToLoop = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howtoplay_loop.png"))
  let howToGame = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howtoplay_game.png"))
  let howToPoint = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howtoplay_point.png"))
  let howToBoard = makeLoadingTarget <| lazy(Texture2D.LoadStrict(@"Menu/howtoplay_board.png"))

  let font = makeLoadingTarget <| lazy(Font.LoadStaticFontStrict(@"Font/Makinas-4-Square/font.a2f"))
  let errorMessageFont = makeLoadingTarget <| lazy(Font.LoadDynamicFontStrict(@"Font/mplus-1c-medium.ttf", 48))

  member x.ProgressCount with get() = progressCount

  member _.Load (progress) =
    let progress a = a |> ignore |> progress

    backgroundTexture.Force() |> progress
    maskTexture.Force() |> progress
    titleTexture.Force() |> progress
    buttonBackground.Force() |> progress
    inputBackground.Force() |> progress
    inputFrame.Force() |> progress
    gameInfoFrame.Force() |> progress
    rankingFrame.Force() |> progress
    controllerBackground.Force() |> progress
    selectionArrow.Force() |> progress
    howToKeyboardShift.Force() |> progress
    howToKeyboardSeparate.Force() |> progress
    howToJoystick.Force() |> progress
    howToSlide.Force() |> progress
    howToRoute.Force() |> progress
    howToLoop.Force() |> progress
    howToGame.Force() |> progress
    howToPoint.Force() |> progress
    howToBoard.Force() |> progress
    font.Force() |> progress
    errorMessageFont.Force() |> progress

  member val TextMap = textMap

  member val BackgroundTexture = backgroundTexture.Value
  member val MaskTexture = maskTexture.Value
  member val TitleTexture = titleTexture.Value
  member val ButtonBackground = buttonBackground.Value
  member val InputBackground = inputBackground.Value
  member val InputFrame = inputFrame.Value
  member val GameInfoFrame = gameInfoFrame.Value
  member val RankingFrame = rankingFrame.Value
  member val ControllerBackground = controllerBackground.Value
  member val SelectionArrow = selectionArrow.Value

  member val HowToKeyboardShift = howToKeyboardShift.Value
  member val HowToKeyboardSeparate = howToKeyboardSeparate.Value
  member val HowToJoystick = howToJoystick.Value

  member val HowToSlide = howToSlide.Value
  member val HowToRoute = howToRoute.Value
  member val HowToLoop =howToLoop.Value
  member val HowToGame = howToGame.Value
  member val HowToPoint = howToPoint.Value
  member val HowToBoard = howToBoard.Value

  member val Font = font.Value
  member val ErrorMessageFont = errorMessageFont.Value

  member val RankingGameMode: Map<GameMode, string> =
    [|
      GameMode.TimeAttack5000, textMap.buttons.timeattack5000
      GameMode.ScoreAttack180, textMap.buttons.scoreattack180
      GameMode.Endless, textMap.buttons.endless
    |]
    |> Array.map(fun (m, s) -> m, sprintf "%s%s" textMap.modes.rankingOf s)
    |> Map.ofArray

  member val MainMenuButtons: string[] =
    Menu.Mode.items |> Array.map (function
      | Menu.Mode.GamePlay -> textMap.buttons.play
      | Menu.Mode.Ranking -> textMap.buttons.ranking
      | Menu.Mode.HowTo -> textMap.buttons.howTo
      | Menu.Mode.Setting -> textMap.buttons.setting
    )

  member val MainMenuDescriptions: string[] =
    Menu.Mode.items |> Array.map (function
      | Menu.Mode.GamePlay -> textMap.descriptions.play
      | Menu.Mode.Ranking -> textMap.descriptions.ranking
      | Menu.Mode.HowTo -> textMap.descriptions.howTo
      | Menu.Mode.Setting -> textMap.descriptions.setting
    )

  member val GameModeButtons: string[] =
    GameMode.items |> Array.map(function
      | GameMode.TimeAttack5000 -> textMap.buttons.timeattack5000
      | GameMode.ScoreAttack180 -> textMap.buttons.scoreattack180
      | GameMode.Endless -> textMap.buttons.endless
    )

  member val GameModeDescriptions: string[] =
    GameMode.items |> Array.map (function
    | GameMode.TimeAttack5000 -> textMap.descriptions.timeattack5000
    | GameMode.ScoreAttack180 -> textMap.descriptions.scoreattack180
    | GameMode.Endless -> textMap.descriptions.endless
  )

  member val SettingMenuButtons: string[] =
    Setting.Mode.items |> Array.map (function
      | Setting.Mode.InputName -> textMap.buttons.namesetting
      | Setting.Mode.Volume -> textMap.buttons.volumeSetting
      | Setting.Mode.Background -> textMap.buttons.backgroundsetting
      | Setting.Mode.Enter -> textMap.buttons.save
    )

  member val SettingModeDescriptions: string[] =
    Setting.Mode.items |> Array.map (function
      | Setting.Mode.InputName -> textMap.descriptions.namesetting
      | Setting.Mode.Volume -> textMap.descriptions.volumeSetting
      | Setting.Mode.Background -> textMap.descriptions.backgroundsetting
      | Setting.Mode.Enter -> textMap.descriptions.settingsave
    )

  member val PauseModeButtons: string[] =
    Pause.PauseSelect.items |> Array.map (function
      | Pause.PauseSelect.Continue -> textMap.buttons.continueGame
      | Pause.PauseSelect.ChangeController -> textMap.buttons.changeController
      | Pause.PauseSelect.Restart -> textMap.buttons.restartGame
      | Pause.PauseSelect.Quit -> textMap.buttons.quitGame
    )

  member val PauseModeDescriptions: string[] =
    Pause.PauseSelect.items |> Array.map (function
      | Pause.PauseSelect.Continue -> textMap.descriptions.continueGame
      | Pause.PauseSelect.ChangeController -> textMap.descriptions.selectController
      | Pause.PauseSelect.Restart -> textMap.descriptions.restartGame
      | Pause.PauseSelect.Quit -> textMap.descriptions.quitGame
    )

  member val GameResultSendToServerButtons: string[] =
    GameResult.SendToServer.items |> Array.map (function
      | GameResult.Yes -> textMap.buttons.sendToServer
      | GameResult.No -> textMap.buttons.notSendToServer
    )

  member val GameResultNextSelectionButtons: string[] =
    GameResult.GameNextSelection.items |> Array.map (function
      | GameResult.GameNextSelection.Quit -> textMap.buttons.quitGame
      | GameResult.GameNextSelection.Restart -> textMap.buttons.restartGame
    )

  member val GameResultNextSelectionDescriptions: string[] =
    GameResult.GameNextSelection.items |> Array.map (function
      | GameResult.GameNextSelection.Quit -> textMap.descriptions.quitGame
      | GameResult.GameNextSelection.Restart -> textMap.descriptions.restartGame
    )

  member val VolumeModeButtons: string[] =
    VolumeSetting.VolumeMode.items |> Array.map (function
      | VolumeSetting.VolumeMode.BGM -> textMap.buttons.bgm
      | VolumeSetting.VolumeMode.SE -> textMap.buttons.se
    )

  member val BackgroundButtons =
    Background.items |> Array.map (function
      | Background.Wave -> textMap.buttons.backgroundWaveBlue
      | Background.FloatingTiles -> textMap.buttons.backgroundFloatingTiles
    )
