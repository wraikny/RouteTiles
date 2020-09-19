namespace RouteTiles.App.MenuV2

open RouteTiles.App
open RouteTiles.Core
open RouteTiles.Core.SubMenu
open RouteTiles.Core.Types

open Altseed2

type internal Container (textMap: TextMap.TextMap) =
  member val TextMap = textMap
  member val BackgroundTexture = Texture2D.LoadStrict(@"Menu/background_dark.png")
  member val MaskTexture = Texture2D.LoadStrict(@"Menu/background_mask.png")
  member val TitleTexture = Texture2D.LoadStrict(@"Menu/title.png")
  member val ButtonBackground = Texture2D.LoadStrict(@"Menu/button-metalic-dark-highlight-320x80.png")
  member val InputBackground = Texture2D.LoadStrict(@"Menu/input_background.png")
  member val InputFrame = Texture2D.LoadStrict(@"Menu/input_frame.png")
  member val GameInfoFrame = Texture2D.LoadStrict(@"Menu/game_info_frame.png")
  member val RankingFrame = Texture2D.LoadStrict(@"Menu/ranking_frame.png")
  member val ControllerBackground = Texture2D.LoadStrict(@"Menu/controller_background.png")
  member val SelectionArrow = Texture2D.LoadStrict(@"Menu/selection_more.png")
  // member val InputUsernameBackground = Texture2D.LoadStrict(@"Menu/input_username.png")
  // member val Font = Font.LoadStaticFontStrict(@"Font/Makinas-4-Square-32/font.a2f")
  member val Font = Font.LoadDynamicFontStrict(@"Font/Makinas-4-Square.otf", 32)

  // member val Font = Font.LoadDynamicFontStrict(@"mplus-1c-bold.ttf", 32)

  member val MainMenuButtons: string[] =
    MenuV2.Mode.items |> Array.map (function
      | MenuV2.Mode.GamePlay -> textMap.buttons.play
      | MenuV2.Mode.Ranking -> textMap.buttons.ranking
      | MenuV2.Mode.Setting -> textMap.buttons.setting
    )

  member val MainMenuDescriptions: string[] =
    MenuV2.Mode.items |> Array.map (function
      | MenuV2.Mode.GamePlay -> textMap.descriptions.play
      | MenuV2.Mode.Ranking -> textMap.descriptions.ranking
      | MenuV2.Mode.Setting -> textMap.descriptions.setting
    )

  member val GameModeButtons: string[] =
    SoloGame.GameMode.items |> Array.map(function
      | SoloGame.GameMode.TimeAttack2000 -> textMap.buttons.timeattack2000
      | SoloGame.GameMode.ScoreAttack180 -> textMap.buttons.scoreattack180
    )

  member val GameModeDescriptions: string[] =
    SoloGame.GameMode.items |> Array.map (function
    | SoloGame.GameMode.TimeAttack2000 -> textMap.descriptions.timeattack2000
    | SoloGame.GameMode.ScoreAttack180 -> textMap.descriptions.scoreattack180
  )

  member val SettingMenuButtons: string[] =
    Setting.Mode.items |> Array.map (function
      | Setting.Mode.InputName -> textMap.buttons.namesetting
      | Setting.Mode.Background -> textMap.buttons.backgroundsetting
      | Setting.Mode.Enter -> textMap.buttons.save
    )

  member val SettingModeDescriptions: string[] =
    Setting.Mode.items |> Array.map (function
      | Setting.Mode.InputName -> textMap.descriptions.namesetting
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
