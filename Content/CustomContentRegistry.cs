using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MinionLib.Content;

public static class CustomContentRegistry
{
    private static readonly object Sync = new();
    private static readonly HashSet<Assembly> RegisteredAssemblies = [];
    private static readonly HashSet<Type> ModelTypes = [];
    private static readonly Dictionary<Type, List<Type>> ModelsByPool = [];

    public static void RegisterAssembly(Assembly assembly)
    {
        lock (Sync)
        {
            if (!RegisteredAssemblies.Add(assembly))
                return;

            foreach (var type in assembly.GetTypes())
                RegisterModelType(type);
        }
    }

    public static void AppendModelSubtypes(ref Type[] modelTypes)
    {
        Type[] customTypes;
        lock (Sync)
            customTypes = ModelTypes.ToArray();

        if (customTypes.Length == 0)
            return;

        modelTypes = modelTypes.Concat(customTypes).Distinct().ToArray();
    }

    public static void AppendCards(CardPoolModel poolModel, ref IEnumerable<CardModel> cards)
    {
        var modelTypes = GetPoolModelTypes<CardModel>(poolModel.GetType());
        if (modelTypes.Length == 0)
            return;

        cards = cards.Concat(modelTypes.Select(GetModel<CardModel>)).Distinct();
    }

    public static void AppendPotions(PotionPoolModel poolModel, ref IEnumerable<PotionModel> potions)
    {
        var modelTypes = GetPoolModelTypes<PotionModel>(poolModel.GetType());
        if (modelTypes.Length == 0)
            return;

        potions = potions.Concat(modelTypes.Select(GetModel<PotionModel>)).Distinct();
    }

    private static void RegisterModelType(Type type)
    {
        if (type.IsAbstract || !typeof(AbstractModel).IsAssignableFrom(type))
            return;

        if (type.GetConstructor(Type.EmptyTypes) == null)
            return;

        ModelTypes.Add(type);

        var poolAttribute = type.GetCustomAttribute<PoolAttribute>();
        if (poolAttribute == null)
            return;

        if (!typeof(AbstractModel).IsAssignableFrom(poolAttribute.PoolType) ||
            !typeof(IPoolModel).IsAssignableFrom(poolAttribute.PoolType))
        {
            throw new InvalidOperationException(
                $"Pool type '{poolAttribute.PoolType.FullName}' for model '{type.FullName}' must be an AbstractModel implementing IPoolModel.");
        }

        if (!ModelsByPool.TryGetValue(poolAttribute.PoolType, out var modelTypes))
        {
            modelTypes = [];
            ModelsByPool.Add(poolAttribute.PoolType, modelTypes);
        }

        if (!modelTypes.Contains(type))
            modelTypes.Add(type);
    }

    private static Type[] GetPoolModelTypes<TModel>(Type poolType)
        where TModel : AbstractModel
    {
        lock (Sync)
        {
            return ModelsByPool.TryGetValue(poolType, out var modelTypes)
                ? modelTypes.Where(t => typeof(TModel).IsAssignableFrom(t)).ToArray()
                : [];
        }
    }

    private static TModel GetModel<TModel>(Type modelType)
        where TModel : AbstractModel
    {
        if (!ModelDb.Contains(modelType))
            ModelDb.Inject(modelType);

        return ModelDb.GetById<TModel>(ModelDb.GetId(modelType));
    }
}

[HarmonyPatch(typeof(ModelDb), "get_AllAbstractModelSubtypes")]
internal static class ModelDbAllAbstractModelSubtypesPatch
{
    private static void Postfix(ref Type[] __result)
    {
        CustomContentRegistry.AppendModelSubtypes(ref __result);
    }
}

[HarmonyPatch(typeof(CardPoolModel), "get_AllCards")]
internal static class CardPoolModelAllCardsPatch
{
    private static void Postfix(CardPoolModel __instance, ref IEnumerable<CardModel> __result)
    {
        CustomContentRegistry.AppendCards(__instance, ref __result);
    }
}

[HarmonyPatch(typeof(PotionPoolModel), "get_AllPotions")]
internal static class PotionPoolModelAllPotionsPatch
{
    private static void Postfix(PotionPoolModel __instance, ref IEnumerable<PotionModel> __result)
    {
        CustomContentRegistry.AppendPotions(__instance, ref __result);
    }
}
