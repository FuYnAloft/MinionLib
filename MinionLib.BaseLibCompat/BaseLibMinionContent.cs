using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace MinionLib.BaseLibCompat;

public static class BaseLibMinionContent
{
    public static void RegisterMinion<TMinion>()
        where TMinion : BaseLibMinionModel, new()
    {
        new TMinion().RegisterSceneConversions();
    }

    public static void AddCard<TPool, TCard>()
        where TPool : CardPoolModel
        where TCard : CardModel
    {
        ModHelper.AddModelToPool<TPool, TCard>();
    }

    public static void AddPotion<TPool, TPotion>()
        where TPool : PotionPoolModel
        where TPotion : PotionModel
    {
        ModHelper.AddModelToPool<TPool, TPotion>();
    }
}
