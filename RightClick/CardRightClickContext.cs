using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MinionLib.RightClick;

public readonly record struct CardRightClickContext(
    NPlayerHand Hand,
    NCardHolder Holder,
    CardModel Card);

