namespace RouteTiles.Core.Types.MenuV2
open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.SubMenu

type State =
  | MainManuState of MainMenu.State
  | SettingMenuState of Setting.State
