module RouteTiles.Core.Types.SubMenu.MainMenu

[<Struct; RequireQualifiedAccess>]
type SoloGameMode = TimeAttack | ScoreAttack

[<Struct; RequireQualifiedAccess>]
type MenuMode =
  | SoloGame of SoloGameMode
  | VS
  | Ranking
  | Achievement
  | Setting
with
  static member TimeAttack = SoloGame SoloGameMode.TimeAttack
  static member ScoreAttack = SoloGame SoloGameMode.ScoreAttack

  member this.IsEnabled = this |> function
    | Achievement | VS -> false
    | _ -> true

type State = {
  cursor: MenuMode
}
