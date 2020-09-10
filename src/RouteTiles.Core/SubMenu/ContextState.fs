module RouteTiles.Core.Types.SubMenu.ContextState
open RouteTiles.Core

open EffFs
open EffFs.Library.StateMachine

[<Struct>]
type State<'context, 'state> = State of 'state
with
  static member inline StateOut(_: State<'c, ^s>): _
    when ^s: (static member StateOut: ^s -> EffectTypeMarker<'o>) =
    Eff.marker<'c * 'o>

// let inline map f (s, k) state = stateMap f (s, fun o -> k (state, o))
let inline mapEff (context: 'c) (f: ^s -> Eff<StateStatus< ^s, 'o>, _>) (State s: State<'c, ^s>, k: 'c*'o -> ^state) =
  eff {
    match! f s with
    | Pending s -> return Internal.callStateEnter k (State s)
    | Completed o -> return k (context, o)
  }
