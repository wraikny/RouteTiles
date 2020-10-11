namespace RouteTiles.Menu.SubMenu
open RouteTiles.Menu

open EffFs
open EffFs.Library.StateMachine

[<Struct>]
type WithContext<'context, 'state> = WithContext of 'state
with
  static member inline StateOut(_: WithContext<'c, ^s>): _
    when ^s: (static member StateOut: ^s -> EffectTypeMarker<'o>) =
    Eff.marker<'c * 'o>

module WithContext =
  // let inline map f (s, k) state = stateMap f (s, fun o -> k (state, o))
  let inline mapEff (context: 'c) (f: ^s -> Eff<StateStatus< ^s, 'o>, _>) (WithContext s: WithContext<'c, ^s>, k: 'c*'o -> ^state) =
    eff {
      match! f s with
      | Pending s -> return Internal.callStateEnter k (WithContext s)
      | Completed o -> return k (context, o)
    }
