module RouteTiles.Menu.SubMenu.Pause

open RouteTiles.Common.Types
open RouteTiles.Menu
open RouteTiles.Menu.Types
open RouteTiles.Menu.SubMenu
open RouteTiles.Menu.Effects

open EffFs
open EffFs.Library.StateMachine

[<Struct; RequireQualifiedAccess>]
type PauseSelect =
  | Continue
  | ChangeController
  | Restart
  | Quit

module PauseSelect =
  let items = [|
    PauseSelect.Continue
    PauseSelect.ChangeController
    PauseSelect.Restart
    PauseSelect.Quit
  |]


[<Struct; RequireQualifiedAccess>]
type PauseResult =
  | Continue of cont:Controller
  | Restart of restart:Controller
  | Quit

type State =
  | Base of Controller * ListSelector.State<PauseSelect>
  | ControllerSelectState of ListSelector.State<Controller> * (Controller voption -> StateStatus<State, PauseResult>)
with
  static member Init (controller) =
    let selector = ListSelector.State<_>.Init(0, PauseSelect.items)
    Base (controller, selector)

  static member StateEnter(s, k) = ControllerSelectState(s, k)

  static member StateOut(_) = Eff.marker<PauseResult>

let equal a b = (a, b) |> function
  | Base (a1, a2), Base (b1, b2) -> (a1, a2) = (b1, b2)
  | ControllerSelectState(a, _), ControllerSelectState(b, _) -> a = b
  | _ -> false

type Msg =
  | Incr
  | Decr
  | Enter
  | Cancel
  | UpdateControllers of Controller[]

module Msg =
  let toListSelectorMsg = function
    | Incr -> ValueSome ListSelector.Msg.Incr
    | Decr -> ValueSome ListSelector.Msg.Decr
    | Enter -> ValueSome ListSelector.Msg.Enter
    | Cancel -> ValueSome ListSelector.Msg.Cancel
    | _ -> ValueNone

let inline update (msg: Msg) (state: State) = eff {
  match msg, state with
  | _, Base (controller, selector) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return Pending state
    | ValueSome msg ->
      match! ListSelector.update msg selector with
      | Pending s -> return Pending <| Base(controller, s)

      | Completed ValueNone
      | Completed (ValueSome PauseSelect.Continue) ->
        return Completed (PauseResult.Continue controller)

      | Completed (ValueSome PauseSelect.Restart) ->
        return Completed (PauseResult.Restart controller)

      | Completed (ValueSome PauseSelect.Quit) ->
        return Completed PauseResult.Quit

      | Completed (ValueSome PauseSelect.ChangeController) ->
        let! controllers = CurrentControllers
        match!
          ListSelector.State<_>.Init(controller, controllers,currentItem=controller)
          |> stateEnter
          with
        | ValueNone -> return Pending state
        | ValueSome c ->
          let! res = SetController c
          if res then
            return Pending (Base(c, selector))
          else
            return Pending state


  | UpdateControllers controllers, ControllerSelectState (s, k) ->
    let controllerSelect =
      let cursorItem = s.selection.[s.cursor]
      let currentItem = s.current |> Option.map(fun i -> s.selection.[i])
      ListSelector.State<_>.Init(cursorItem, controllers, ?currentItem=currentItem)
    return Pending <| ControllerSelectState (controllerSelect, k)

  | _, ControllerSelectState (s, k) ->
    match Msg.toListSelectorMsg msg with
    | ValueNone -> return Pending state
    | ValueSome msg -> return! stateMapEff (ListSelector.update msg) (s, k)
}