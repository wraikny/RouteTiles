namespace RouteTiles.App



open Altseed2

type PauseNode() =
  inherit Node()

  let coroutineNode = CoroutineNode()

  do
    base.AddChildNode(
      RectangleNode(
        Size = Consts.windowSize.To2F(),
        Color = Consts.Color.pauseBackground,
        ZOrder = ZOrder.Pause.background
      )
    )

  do
    coroutineNode.Add(seq {
      while true do
        
        yield()
    })
