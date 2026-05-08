using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MinionLib.Minion;
using STS2RitsuLib.Content;

namespace MinionLib.RitsuCompat;

public static class RitsuMinionContent
{
    public static void RegisterMinion<TMinion>(ModContentRegistry registry)
        where TMinion : MinionModel
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.RegisterMonster<TMinion>();
    }

    public static void RegisterCard<TPool, TCard>(ModContentRegistry registry)
        where TPool : CardPoolModel
        where TCard : CardModel
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.RegisterCard<TPool, TCard>();
    }

    public static void RegisterPotion<TPool, TPotion>(ModContentRegistry registry)
        where TPool : PotionPoolModel
        where TPotion : PotionModel
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.RegisterPotion<TPool, TPotion>();
    }

    public static void RegisterPower<TPower>(ModContentRegistry registry)
        where TPower : PowerModel
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.RegisterPower<TPower>();
    }
}
