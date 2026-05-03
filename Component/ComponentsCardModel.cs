using System.ComponentModel;
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

#pragma warning disable CS0809
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

    public ICardComponent? AddComponent<T>(T incoming, bool allowMerge = true, bool isUpgrade = false)
        where T : class, ICardComponent
    {
        return ApplyComponent(incoming, new ApplyComponentOptions(
            AllowMerge: allowMerge,
            UseSubtractiveMerge: false,
            IsUpgrade: isUpgrade
        ));
    }

    public ICardComponent? SubtractComponent<T>(T incoming, bool isUpgrade = false)
        where T : class, ICardComponent
    {
        return ApplyComponent(incoming, new ApplyComponentOptions(
            AllowMerge: true,
            UseSubtractiveMerge: true,
            IsUpgrade: isUpgrade
        ));
    }

    public ICardComponent? ApplyComponent<T>(T incoming, ApplyComponentOptions options = new())
        where T : class, ICardComponent
    {
        EnsureComponentsInitialized();
        if (options.AllowMerge)
        {
            for (var i = 0; i < _components!.Count; i++)
            {
                var existing = _components[i];
                var didMerge = options.UseSubtractiveMerge
                    ? existing.TrySubtractiveMergeWith(incoming, options, out var merged)
                    : existing.TryMergeWith(incoming, options, out merged);

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

                _components[i] = merged;
                merged.Attach(this);
                return merged;
            }
        }

        if (options.UseSubtractiveMerge) return null;
        _components!.Add(incoming);
        incoming.Attach(this);
        return incoming;
    }

    public ICardComponent? RemoveComponent<T>() where T : class, ICardComponent
    {
        EnsureComponentsInitialized();

        var index = _components!.FindIndex(c => c is T);
        if (index < 0)
            return null;

        var removed = _components[index];
        _components[index].Detach();
        _components.RemoveAt(index);
        return removed;
    }

    public IReadOnlyList<ICardComponent> RemoveComponents<T>() where T : class, ICardComponent
    {
        EnsureComponentsInitialized();

        List<ICardComponent> removed = [];
        for (var i = _components!.Count - 1; i >= 0; i--)
        {
            if (_components[i] is not T component)
                continue;

            component.Detach();
            _components.RemoveAt(i);
            removed.Add(component);
        }

        removed.Reverse();

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

    public IReadOnlyList<T> GetComponents<T>() where T : class, ICardComponent
    {
        EnsureComponentsInitialized();
        return _components!.OfType<T>().ToArray();
    }

    public void EnsureComponentsInitialized()
    {
        if (_components != null)
            return;

        if (_componentStateBlob.Length == 0)
        {
            _components = [];
            foreach (var canonicalComponent in CanonicalComponents)
            {
                var component = canonicalComponent.DeepClone();
                _components.Add(component);
                component.Attach(this);
            }
        }
        else
        {
            _components = CardComponentStateSerializer.Deserialize(_componentStateBlob, this);
        }

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
        var count = _components!.Count;
        for (var displayIndex = 0; displayIndex < count; displayIndex++)
        {
            var component = _components[displayIndex];
            var args = common.ToDictionary();
            args["ComponentPosition"] = displayIndex;
            args["ComponentPositionFromEnd"] = count - 1 - displayIndex;
            args["IsFirstComponent"] = displayIndex == 0;
            args["IsLastComponent"] = displayIndex == count - 1;
            prefixSb.Append(component.GetFormattedPrefix(args));
        }

        for (var displayIndex = 0; displayIndex < count; displayIndex++)
        {
            var component = _components[count - 1 - displayIndex];
            var args = common.ToDictionary();
            args["ComponentPosition"] = displayIndex;
            args["ComponentPositionFromEnd"] = count - 1 - displayIndex;
            args["IsFirstComponent"] = displayIndex == 0;
            args["IsLastComponent"] = displayIndex == count - 1;
            postfixSb.Append(component.GetFormattedPostfix(args));
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
            : IsUpgraded
                ? UpgradeDisplay.Upgraded
                : UpgradeDisplay.Normal;
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
        foreach (var component in _components!.ToArray())
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This member is sealed. Try adding `ComponentContext componentContext` as the last parameter, or disable this warning if intended.", false)]
    protected sealed override void OnUpgrade()
    {
        EnsureComponentsInitialized();
        var context = new ComponentContext(ComponentPhase.Core);
        foreach (var component in _components!.ToArray())
            component.OnUpgrade(context);
        OnUpgrade(context);
    }

    protected virtual void OnUpgrade(ComponentContext componentContext) { }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This member is sealed. Try adding `ComponentContext componentContext` as the last parameter, or disable this warning if intended.", false)]
    protected sealed override void AfterDowngraded()
    {
        EnsureComponentsInitialized();
        var context = new ComponentContext(ComponentPhase.Core);
        foreach (var component in _components!.ToArray())
            component.AfterDowngraded(context);
        AfterDowngraded(context);
    }

    protected virtual void AfterDowngraded(ComponentContext componentContext) { }
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
