using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.RightClick;

public record RightClickContext(Player Player, AbstractModel Model);