namespace RouteTiles.App.MenuV2

open RouteTiles.Core
open RouteTiles.Core.SubMenu
open RouteTiles.App

open Altseed2

type Container (textMap: TextMap.TextMap) =
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

  member val MainMenuButtons =
    MenuV2.Mode.items |> Array.map (function
      | MenuV2.Mode.GamePlay -> textMap.buttons.play
      | MenuV2.Mode.Ranking -> textMap.buttons.ranking
      | MenuV2.Mode.Setting -> textMap.buttons.setting
    )

  member val MainMenuDescriptions =
    MenuV2.Mode.items |> Array.map (function
      | MenuV2.Mode.GamePlay -> textMap.descriptions.play
      | MenuV2.Mode.Ranking -> textMap.descriptions.ranking
      | MenuV2.Mode.Setting -> textMap.descriptions.setting
    )

  member val GameModeButtons =
    MenuV2.GameMode.items |> Array.map(function
      | MenuV2.GameMode.TimeAttack2000 -> textMap.buttons.timeattack2000
      | MenuV2.GameMode.ScoreAttack180 -> textMap.buttons.scoreattack180
    )

  member val GameModeDescriptions =
    MenuV2.GameMode.items |> Array.map (function
    | MenuV2.GameMode.TimeAttack2000 -> textMap.descriptions.timeattack2000
    | MenuV2.GameMode.ScoreAttack180 -> textMap.descriptions.scoreattack180
  )

  member val SettingMenuButtons =
    Setting.Mode.items |> Array.map (function
      | Setting.Mode.InputName -> textMap.buttons.namesetting
      | Setting.Mode.Background -> textMap.buttons.backgroundsetting
      | Setting.Mode.Enter -> textMap.buttons.save
    )

  member val SettingModeDescriptions =
    Setting.Mode.items |> Array.map (function
      | Setting.Mode.InputName -> textMap.descriptions.namesetting
      | Setting.Mode.Background -> textMap.descriptions.backgroundsetting
      | Setting.Mode.Enter -> textMap.descriptions.settingsave
    )

  member val PauseModeButtons =
    MenuV2.PauseSelect.items |> Array.map (function
      | MenuV2.Continue -> textMap.buttons.continueGame
      | MenuV2.ChangeController -> textMap.buttons.changeController
      | MenuV2.Restart -> textMap.buttons.restartGame
      | MenuV2.Quit -> textMap.buttons.quitGame
    )

  member val PauseModeDescriptions =
    MenuV2.PauseSelect.items |> Array.map (function
      | MenuV2.Continue -> textMap.descriptions.continueGame
      | MenuV2.ChangeController -> textMap.descriptions.changeController
      | MenuV2.Restart -> textMap.descriptions.restartGame
      | MenuV2.Quit -> textMap.descriptions.quitGame
    )