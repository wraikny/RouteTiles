module RouteTiles.Core.Types.Pause

[<Struct; RequireQualifiedAccess>]
type Model =
  | ContinueGame
  | RestartGame
  | QuitGame
  | NotPaused