namespace RouteTiles.App.Menu

open RouteTiles.App
open RouteTiles.Common
open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Menu.SubMenu
open RouteTiles.Menu.Types
open RouteTiles.Menu

open Altseed2

type internal Container (textMap: TextMap.TextMap, progress: unit -> int) =
  let withProgress x = progress () |> ignore; x

  static member val ProgressCount = 22

  member val TextMap = textMap

  member val BackgroundTexture = Texture2D.LoadStrict(@"Menu/background_dark.png") |> withProgress
  member val MaskTexture = Texture2D.LoadStrict(@"Menu/background_mask.png") |> withProgress
  member val TitleTexture = Texture2D.LoadStrict(@"Menu/title.png") |> withProgress
  member val ButtonBackground = Texture2D.LoadStrict(@"Menu/button-metalic-dark-highlight-320x80.png") |> withProgress
  member val InputBackground = Texture2D.LoadStrict(@"Menu/input_background.png") |> withProgress
  member val InputFrame = Texture2D.LoadStrict(@"Menu/input_frame.png") |> withProgress
  member val GameInfoFrame = Texture2D.LoadStrict(@"Menu/game_info_frame.png") |> withProgress
  member val RankingFrame = Texture2D.LoadStrict(@"Menu/ranking_frame.png") |> withProgress
  member val ControllerBackground = Texture2D.LoadStrict(@"Menu/controller_background.png") |> withProgress
  member val SelectionArrow = Texture2D.LoadStrict(@"Menu/selection_more.png") |> withProgress

  member val HowToKeyboardShift = Texture2D.LoadStrict(@"Menu/howto_keyboard_shift.png") |> withProgress
  member val HowToKeyboardSeparate = Texture2D.LoadStrict(@"Menu/howto_keyboard_separate.png") |> withProgress
  member val HowToJoystick = Texture2D.LoadStrict(@"Menu/howto_joystick.png") |> withProgress

  member val HowToSlide = Texture2D.LoadStrict(@"Menu/howtoplay_slide.png") |> withProgress
  member val HowToRoute = Texture2D.LoadStrict(@"Menu/howtoplay_route.png") |> withProgress
  member val HowToLoop = Texture2D.LoadStrict(@"Menu/howtoplay_loop.png") |> withProgress
  member val HowToGame = Texture2D.LoadStrict(@"Menu/howtoplay_game.png") |> withProgress
  member val HowToPoint = Texture2D.LoadStrict(@"Menu/howtoplay_point.png") |> withProgress
  member val HowToBoard = Texture2D.LoadStrict(@"Menu/howtoplay_board.png") |> withProgress

  // member val InputUsernameBackground = Texture2D.LoadStrict(@"Menu/input_username.png")
  member val Font = Font.LoadStaticFontStrict(@"Font/Makinas-4-Square-32/font.a2f") |> withProgress
  member val ErrorMessageFont = Font.LoadDynamicFontStrict(@"Font/mplus-1c-medium.ttf", 24) |> withProgress
  member val DynamicFont = Font.LoadDynamicFontStrict(@"Font/Makinas-4-Square.otf", 32) |> withProgress

  // member val Font = Font.LoadDynamicFontStrict(@"mplus-1c-bold.ttf", 32)

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
