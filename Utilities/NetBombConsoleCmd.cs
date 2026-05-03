#if DEBUG || EXPORTDEBUG
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.ValueProps;

namespace MinionLib.Utilities;

public class NetBombConsoleCmd : AbstractConsoleCmd
{
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (!CombatManager.Instance.IsInProgress)
            return new CmdResult(false, "This doesn't appear to be a combat!");
        var state = CombatManager.Instance.DebugOnlyGetState();
        var task = CreatureCmd.GainBlock(LocalContext.GetMe(state)!.Creature, 5, ValueProp.Unpowered, null);
        return new CmdResult(task, true, "**You** gain 5 block, which will bomb the net combat");
    }

    public override string CmdName => "netbomb";

    public override string Args => "";

    public override string Description => "Bomb the net combat";

    public override bool IsNetworked => true;
}
#endif
