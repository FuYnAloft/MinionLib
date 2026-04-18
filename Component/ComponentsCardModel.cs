using System.Text;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;
using MinionLib.Targeting.Utilities;
using MinionLib.Utilities.BetterExtraArgs;

namespace MinionLib.Component;

public abstract partial class ComponentsCardModel(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : CardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary),
        IComponentsCardModel, IEasyRightClickableCard, IBetterAddExtraArgsCard
{
    // ReSharper disable once ConvertToConstant.Local
    private static readonly int MaxPhaseTransitions = 64;

    private List<ICardComponent>? _components;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int[] MinionLibComponentStateBlob
    {
        get
        {
            if (_components != null)
                _componentStateBlob = CardComponentStateSerializer.Serialize(_components);

            return _componentStateBlob;
        }
        set
        {
            _componentStateBlob = value;
            _components = null;
        }
    }

    private int[] _componentStateBlob = [];

    public IReadOnlyList<ICardComponent> Components
    {
        get
        {
            EnsureComponentsInitialized();
            return _components!;
        }
    }

    protected virtual IEnumerable<ICardComponent> CanonicalComponents => [];

    public ICardComponent? AddComponent<T>(T incoming, bool allowMerge = true,
        bool useSubtractiveMerge = false) where T : class, ICardComponent
    {
        EnsureComponentsInitialized();
        if (allowMerge)
        {
            for (var i = 0; i < _components!.Count; i++)
            {
                var existing = _components[i];
                var didMerge = useSubtractiveMerge
                    ? existing.TrySubtractiveMergeWith(incoming, out var merged)
                    : existing.TryMergeWith(incoming, out merged);

                if (!didMerge)
                    continue;

                if (ReferenceEquals(merged, existing))
                    return existing;

                existing.Detach();

                if (merged == null)
                {
                    _components.RemoveAt(i);
                    return null;
                }

                merged.Attach(this);
                _components[i] = merged;
                return merged;
            }
        }

        if (useSubtractiveMerge) return null;
        incoming.Attach(this);
        _components!.Add(incoming);
        return incoming;
    }

    public bool RemoveComponent<T>() where T : class, ICardComponent
    {
        EnsureComponentsInitialized();

        var index = _components!.FindIndex(c => c is T);
        if (index < 0)
            return false;

        _components[index].Detach();
        _components.RemoveAt(index);
        return true;
    }

    public int RemoveComponents<T>() where T : class, ICardComponent
    {
        EnsureComponentsInitialized();

        var removed = 0;
        for (var i = _components!.Count - 1; i >= 0; i--)
        {
            if (_components[i] is not T component)
                continue;

            component.Detach();
            _components.RemoveAt(i);
            removed++;
        }

        return removed;
    }

    public bool RefRemoveComponent(ICardComponent component)
    {
        EnsureComponentsInitialized();

        var index = _components!.FindIndex(c => ReferenceEquals(c, component));
        if (index < 0)
            return false;

        _components[index].Detach();
        _components.RemoveAt(index);
        return true;
    }

    public T? GetComponent<T>() where T : class, ICardComponent
    {
        EnsureComponentsInitialized();
        return _components!.OfType<T>().FirstOrDefault();
    }

    public IEnumerable<T> GetComponents<T>() where T : class, ICardComponent
    {
        EnsureComponentsInitialized();
        return _components!.OfType<T>().ToArray();
    }

    public void EnsureComponentsInitialized()
    {
        if (_components != null)
            return;

        _components = _componentStateBlob.Length == 0
            ? BuildComponentsFromCanonical()
            : CardComponentStateSerializer.Deserialize(_componentStateBlob, this);

        foreach (var component in _components)
            component.Attach(this);

        _componentStateBlob = CardComponentStateSerializer.Serialize(_components);
    }

    public virtual void BetterAddExtraArgsToDescription(
        LocString description,
        PileType pileType,
        DescriptionPreviewType previewType,
        Creature? target = null)
    {
        EnsureComponentsInitialized();
        var common = GenerateCommonExtraArgsForComponents(pileType, previewType, target);
        var prefixSb = new StringBuilder();
        var postfixSb = new StringBuilder();
        foreach (var (index, component) in _components!.Index())
        {
            var args = common.ToDictionary();
            args["ComponentPosition"] = index;
            args["ComponentPositionFromEnd"] = _components!.Count - 1 - index;
            args["IsFirstComponent"] = index == 0;
            args["IsLastComponent"] = index == _components.Count - 1;
            prefixSb.Append(component.GetFormattedPrefix(args)).Append('\u200b');
        }
        foreach (var (index, component) in _components!.Index())
        {
            var args = common.ToDictionary();
            args["ComponentPosition"] = index;
            args["ComponentPositionFromEnd"] = _components!.Count - 1 - index;
            args["IsFirstComponent"] = index == 0;
            args["IsLastComponent"] = index == _components.Count - 1;
            postfixSb.Append(component.GetFormattedPostfix(args)).Append('\u200b');
        }
        
        description.Add("CompPre", prefixSb.ToString());
        description.Add("CompPost", postfixSb.ToString());
    }

    protected virtual Dictionary<string, object> GenerateCommonExtraArgsForComponents(
        PileType pileType,
        DescriptionPreviewType previewType,
        Creature? target = null)
    {
        var args = new Dictionary<string, object>();
        var upgradeDisplay = previewType == DescriptionPreviewType.Upgrade
            ? UpgradeDisplay.UpgradePreview
            : IsUpgraded ? UpgradeDisplay.Upgraded : UpgradeDisplay.Normal;
        args[IfUpgradedVar.defaultName] = new IfUpgradedVar(upgradeDisplay);
        
        var isOnTable = pileType is PileType.Hand or PileType.Play;
        args["OnTable"] = isOnTable;
        
        var inCombat = CombatManager.Instance.IsInProgress && 
                        (Pile?.IsCombatPile ?? pileType.IsCombatPile());
        args["InCombat"] = inCombat;
        
        args["IsTargeting"] = target != null;
        args["TargetType"] = TargetType.ToString();
        var prefix = EnergyIconHelper.GetPrefix(this);
        args["energyPrefix"] = prefix;
        args["singleStarIcon"] = "[img]res://images/packed/sprite_fonts/star_icon.png[/img]";
        return args;
    }

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();

        if (_components != null)
        {
            _components = _components.Select(c => c.DeepClone()).ToList();
            foreach (var component in _components)
                component.Attach(this, true);

            _componentStateBlob = CardComponentStateSerializer.Serialize(_components);
        }
    }

    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();

        _components = null;
        EnsureComponentsInitialized();
    }

    private List<ICardComponent> BuildComponentsFromCanonical()
    {
        return CanonicalComponents.Select(c => c.DeepClone()).ToList();
    }

    # region Deprecated

    [Obsolete(
        "This method is deprecated and should not be called or overridden. Use interface constraints or delegate registry instead.",
        false)]
    public virtual Task ComponentCallBack(string name, params object?[] args)
    {
        return Task.CompletedTask;
    }

    [Obsolete(
        "This method is deprecated and should not be called or overridden. Use interface constraints or delegate registry instead.",
        false)]
    public virtual bool ComponentPredicate(string name, params object?[] args)
    {
        return false;
    }

    [Obsolete(
        "This method is deprecated and should not be called or overridden. Use interface constraints or delegate registry instead.",
        false)]
    public virtual object? ComponentQuery(string name, params object?[] args)
    {
        return null;
    }

    [Obsolete(
        "This method is deprecated and should not be called or overridden. Use interface constraints or delegate registry instead.",
        false)]
    public virtual Task<object?> ComponentQueryAsync(string name, params object?[] args)
    {
        return Task.FromResult<object?>(null);
    }

    #endregion

    protected sealed override bool ShouldGlowGoldInternal =>
        (_components?.Any(c => c.ShouldGlowGoldInternal) ?? false) || ShouldGlowGoldInternalC;

    protected virtual bool ShouldGlowGoldInternalC => false;

    protected sealed override bool ShouldGlowRedInternal =>
        (_components?.Any(c => c.ShouldGlowRedInternal) ?? false) || ShouldGlowRedInternalC;

    protected virtual bool ShouldGlowRedInternalC => false;

    public Color? GlowColor =>
        _components?.Select(c => c.GlowColor).FirstOrDefault(c => c.HasValue) ?? GlowColorC;

    protected virtual Color? GlowColorC => null;

    public override CardType Type =>
        _components?.Select(c => c.CardTypeOverride).FirstOrDefault(t => t.HasValue) ?? base.Type;

    public override CardRarity Rarity =>
        _components?.Select(c => c.CardRarityOverride).FirstOrDefault(r => r.HasValue) ?? base.Rarity;

    public override TargetType TargetType =>
        SingleTargetTypesUnionManager.GetWithBase(
            _components?.Select(c => c.TargetTypeOverride).OfType<TargetType>() ?? [],
            base.TargetType);

    protected sealed override bool IsPlayable =>
        (_components?.All(c => c.IsPlayable) ?? true) && IsPlayableC;

    protected virtual bool IsPlayableC => true;

    protected override PileType GetResultPileType()
    {
        EnsureComponentsInitialized();
        foreach (var component in _components!)
            if (component.GetResultPileType() is { } t)
                return t;
        return base.GetResultPileType();
    }

    public sealed override bool HasTurnEndInHandEffect =>
        (_components?.Any(c => c.HasTurnEndInHandEffect) ?? false) || HasTurnEndInHandEffectC;

    protected virtual bool HasTurnEndInHandEffectC => false;

    protected sealed override IEnumerable<IHoverTip> ExtraHoverTips =>
        _components?.SelectMany(c => c.HoverTips).Concat(ExtraHoverTipsC) ?? ExtraHoverTipsC;

    protected virtual IEnumerable<IHoverTip> ExtraHoverTipsC => [];

    private void HandlePhaseTransitionLimitExceeded(ComponentPhase lastPhase)
    {
        Log.Warn($"""
                  Card '{Id.Entry}' exceeded the maximum of {MaxPhaseTransitions} phase transitions. Last phase: {lastPhase}.
                         This likely indicates an infinite loop in the card's logic, and no further phase transitions will be processed to prevent game instability.
                         At the time, there are {_components!.Count} component(s) attached to the card, with the following types:
                         {string.Join(", ", _components.Select(c => c.ComponentId))}
                         If you are sure it's a false positive, try modify ComponentsCardModel.MaxPhaseTransitions via reflection.
                  """);
    }

    public bool CanHandleRightClickLocal(RightClickContext context)
    {
        EnsureComponentsInitialized();
        return _components!.Any(c => c.CanHandleRightClickLocal(context)) || CanHandleRightClickLocalC(context);
    }

    protected virtual bool CanHandleRightClickLocalC(RightClickContext context) => false;

    public async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        EnsureComponentsInitialized();

        var flag = false;
        foreach (var component in _components!)
        {
            if (component.CanHandleRightClick(clickContext))
            {
                flag = true;
                await component.OnRightClick(choiceContext, clickContext);
                break;
            }
        }

        if (!flag)
            await OnRightClickC(choiceContext, clickContext);
    }

    protected virtual Task OnRightClickC(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        return Task.CompletedTask;
    }
}

public abstract class CustomComponentsCardModel : ComponentsCardModel, ICustomModel, ILocalizationProvider
{
    protected CustomComponentsCardModel(
        int canonicalEnergyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool shouldShowInCardLibrary = true,
        bool autoAdd = true)
        : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
        if (!autoAdd)
            return;
        CustomContentDictionary.AddModel(this.GetType());
    }

    public virtual Texture2D? CustomFrame => null;

    public virtual string? CustomPortraitPath => null;

    public virtual Texture2D? CustomPortrait => null;

    public virtual List<(string, string)>? Localization => null;
}