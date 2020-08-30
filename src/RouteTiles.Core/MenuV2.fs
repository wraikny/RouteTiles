module RouteTiles.Core.Types.MenuV2
open RouteTiles.Core
open RouteTiles.Core.Types
open RouteTiles.Core.Types.SubMenu

open EffFs
open EffFs.Library

type State =
  | MainManuState of MainMenu.State
  | SettingMenuState of Setting.State * (Config -> State)
with
  static member StateEnter(s, k) = SettingMenuState (s, k)

  member x.IsStringInputMode = x |> function
    | SettingMenuState (s, _) -> s.IsStringInputMode
    | _ -> false


[<Struct>]
type Msg =
  | Dir of dir:Dir
  | Enter
  | Cancel
  | MsgOfInput of msgInput:StringInput.Msg


module Msg =
  let toSettingMsg = function
    | Dir Dir.Right -> Setting.Msg.NextMode
    | Dir Dir.Left -> Setting.Msg.PrevMode
    | Dir Dir.Up -> Setting.Msg.Decr
    | Dir Dir.Down -> Setting.Msg.Incr
    | Enter -> Setting.Msg.Enter
    | Cancel -> Setting.Msg.Cancel
    | MsgOfInput m -> Setting.Msg.MsgOfInput m

  let toMainMenuMsg = function
    | Dir d -> MainMenu.Msg.Dir d |> ValueSome
    | Enter -> MainMenu.Msg.Enter |> ValueSome
    | _ -> ValueNone


let inline update (msg: Msg) (state: State): Eff<State, _> = eff {
  match state with
  | MainManuState s ->
    match Msg.toMainMenuMsg msg with
    | ValueNone -> return state

    | ValueSome msg ->
      match! MainMenu.update msg s with
      | StateMachine.Pending s -> return MainManuState s

      | StateMachine.Completed mode ->
        match mode with
        | MainMenu.Mode.SoloGame _gameMode ->
          return state

        | MainMenu.Mode.Setting ->
          // let! config = StateMachine.stateEnter <| Setting.State.Init (s.config)
          // return (
          //   if config = s.config then state
          //   else MainManuState { s with config = config }
          // )
          return SettingMenuState (Setting.State.Init (s.config),
            fun config ->
              if config = s.config then state
              else MainManuState { s with config = config }
          )

        | _ ->
          return state

  | SettingMenuState (s, k) ->
    let msg = Msg.toSettingMsg msg
    return! StateMachine.stateMapEff (Setting.update msg) (s, k)
}
