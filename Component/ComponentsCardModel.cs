using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component;

public abstract partial class ComponentsCardModel(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : CardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary),
        IComponentsCardModel
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

    public T? AddComponent<T>(T component) where T : ICardComponent
    {
        EnsureComponentsInitialized();
        var finalComponent = AddOrMergeComponent(component);
        return finalComponent;
    }

    public bool RemoveComponent<T>() where T : ICardComponent
    {
        EnsureComponentsInitialized();

        var index = _components!.FindIndex(c => c is T);
        if (index < 0)
            return false;

        _components[index].Detach();
        _components.RemoveAt(index);
        return true;
    }

    public int RemoveComponents<T>() where T : ICardComponent
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

    public T? GetComponent<T>() where T : ICardComponent
    {
        EnsureComponentsInitialized();
        return _components!.OfType<T>().FirstOrDefault();
    }

    public IEnumerable<T> GetComponents<T>() where T : ICardComponent
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
            Attach(component, this);

        _componentStateBlob = CardComponentStateSerializer.Serialize(_components);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        EnsureComponentsInitialized();
        var prefixText = string.Join("\u200b",
            Components.Select(c => c.GetFormattedPrefix()).Where(s => !string.IsNullOrWhiteSpace(s)));
        var postfixText = string.Join("\u200b",
            Components.Select(c => c.GetFormattedPostfix()).Where(s => !string.IsNullOrWhiteSpace(s)));
        description.Add("CompPre", prefixText);
        description.Add("CompPost", postfixText);
    }

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();

        if (_components != null)
        {
            _components = _components.Select(c => c.DeepClone()).ToList();
            foreach (var component in _components)
                Attach(component, this);

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

    private T? AddOrMergeComponent<T>(T incoming) where T : ICardComponent
    {
        var existingIndex = _components!.FindIndex(c => c is T);
        if (existingIndex < 0)
        {
            incoming.Attach(this);
            _components.Add(incoming);
            return incoming;
        }

        var existing = _components[existingIndex];
        var merged = existing.MergeWith(incoming);

        if (ReferenceEquals(merged, KeepsTwo.Instance))
        {
            incoming.Attach(this);
            _components.Add(incoming);
            return incoming;
        }

        if (ReferenceEquals(merged, existing)) return (T)merged;

        existing.Detach();

        if (merged == null)
        {
            _components.RemoveAt(existingIndex);
            return default;
        }

        merged.Attach(this);
        _components[existingIndex] = merged;
        return (T)merged;
    }

    private static void Attach(ICardComponent component, IComponentsCardModel owner)
    {
        component.Attach(owner);
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