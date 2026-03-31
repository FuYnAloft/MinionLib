using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace MinionLib.Component;

public abstract class ComponentsCardModel(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true,
    bool autoAdd = true)
    : CustomCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary, autoAdd),
        IComponentsCardModel
{
    private const int MaxPhaseTransitions = 32;

    private List<ICardComponent>? _components;

    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public string MinionLibComponentStateBlob
    {
        get
        {
            if (_components != null)
                _componentStateBlob = CardComponentStateSerializer.Serialize(_components);

            return _componentStateBlob;
        }
        set
        {
            _componentStateBlob = value ?? "";
            _components = null;
        }
    }

    private string _componentStateBlob = string.Empty;

    public IReadOnlyList<ICardComponent> Components
    {
        get
        {
            EnsureComponentsInitialized();
            return _components!;
        }
    }

    public virtual IEnumerable<ICardComponent> CanonicalComponents => [];

    public T AddComponent<T>(T component) where T : ICardComponent
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

        _components = string.IsNullOrWhiteSpace(_componentStateBlob)
            ? BuildComponentsFromCanonical()
            : CardComponentStateSerializer.Deserialize(_componentStateBlob, this);

        foreach (var component in _components)
            Attach(component, this);

        _componentStateBlob = CardComponentStateSerializer.Serialize(_components);
    }

    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        EnsureComponentsInitialized();

        var componentContext = new ComponentContext(ComponentPhase.Init);

        for (var transitions = 0;
             transitions < MaxPhaseTransitions && componentContext.Phase != ComponentPhase.Final;
             transitions++)
        {
            componentContext.MoveNextPhase();

            switch (componentContext.Phase)
            {
                case ComponentPhase.Prefix:
                    foreach (var component in Components.ToArray())
                    {
                        await component.OnPlayPrefix(choiceContext, cardPlay, componentContext);
                        if (componentContext.Phase != ComponentPhase.Prefix) break;
                    }

                    break;
                case ComponentPhase.Postfix:
                    foreach (var component in Components.ToArray())
                    {
                        await component.OnPlayPostfix(choiceContext, cardPlay, componentContext);
                        if (componentContext.Phase != ComponentPhase.Postfix) break;
                    }

                    break;
                case ComponentPhase.Prime:
                case ComponentPhase.Core:
                case ComponentPhase.Final:
                    await OnPlayPhased(choiceContext, cardPlay, componentContext);
                    break;
                case ComponentPhase.Init:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (componentContext.Phase != ComponentPhase.Final)
            throw new InvalidOperationException(
                $"Component phase transition exceeded {MaxPhaseTransitions}. Last phase: {componentContext.Phase}");
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
        var result = new List<ICardComponent>();
        foreach (var canonicalComponent in CanonicalComponents)
        {
            var incoming = canonicalComponent.DeepClone();
            var existingIndex = result.FindIndex(c => c.GetType() == incoming.GetType());
            if (existingIndex < 0)
            {
                result.Add(incoming);
                continue;
            }

            var existing = result[existingIndex];
            var merged = existing.MergeWith(incoming);
            if (merged == null)
                result.RemoveAt(existingIndex);
            else if (ReferenceEquals(merged, KeepsTwo.Instance))
                result.Add(incoming);
            else
                result[existingIndex] = merged;
        }

        return result;
    }

    private T AddOrMergeComponent<T>(T incoming) where T : ICardComponent
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

        existing.Detach();
        incoming.Detach();

        if (merged == null)
        {
            _components.RemoveAt(existingIndex);
            return incoming;
        }

        merged.Attach(this);
        _components[existingIndex] = merged;
        return (T)merged;
    }

    private static void Attach(ICardComponent component, IComponentsCardModel owner)
    {
        component.Attach(owner);
    }
    
    public virtual Task ComponentCallBack(string name, params object[] args)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPlayPhased(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (componentContext.Phase == ComponentPhase.Core)
            return OnPlay(choiceContext, cardPlay, componentContext);

        return Task.CompletedTask;
    }

    public virtual Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }
}