using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

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
    public string MinionLib_ComponentStateBlob
    {
        get
        {
            if (_components != null)
                _componentStateBlob = CardComponentStateSerializer.Serialize(_components);

            return _componentStateBlob;
        }
        private set
        {
            _componentStateBlob = value ?? string.Empty;
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

    public ICardComponent AddComponent(ICardComponent component)
    {
        EnsureComponentsInitialized();
        var finalComponent = AddOrMergeComponent(component);
        MarkComponentStateDirty();
        return finalComponent;
    }

    public bool RemoveComponent<T>() where T : ICardComponent
    {
        EnsureComponentsInitialized();

        var index = _components!.FindIndex(c => c is T);
        if (index < 0)
            return false;

        InvokeNoArg(_components[index], "Detach");
        _components.RemoveAt(index);
        MarkComponentStateDirty();
        return true;
    }

    public T? GetComponent<T>() where T : class, ICardComponent
    {
        EnsureComponentsInitialized();
        return _components!.OfType<T>().FirstOrDefault();
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
                        await component.OnPlayPrefix(choiceContext, cardPlay, componentContext);
                    break;
                case ComponentPhase.Postfix:
                    foreach (var component in Components.ToArray())
                        await component.OnPlayPostfix(choiceContext, cardPlay, componentContext);
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

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        EnsureComponentsInitialized();
        var prefixText = string.Join("\n",
            Components.Select(c => c.GetFormattedPrefix()).Where(s => !string.IsNullOrWhiteSpace(s)));
        var postfixText = string.Join("\n",
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

    protected void MarkComponentStateDirty()
    {
        EnsureComponentsInitialized();
        _componentStateBlob = CardComponentStateSerializer.Serialize(_components!);
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
            else
                result[existingIndex] = merged;
        }

        return result;
    }

    private ICardComponent AddOrMergeComponent(ICardComponent incoming)
    {
        var existingIndex = _components!.FindIndex(c => c.GetType() == incoming.GetType());
        if (existingIndex < 0)
        {
            incoming.Attach(this);
            _components.Add(incoming);
            return incoming;
        }

        var existing = _components[existingIndex];
        var merged = existing.MergeWith(incoming);
        existing.Detach();
        incoming.Detach();

        if (merged == null)
        {
            _components.RemoveAt(existingIndex);
            return incoming;
        }

        merged.Attach(this);
        _components[existingIndex] = merged;
        return merged;
    }

    private static void Attach(ICardComponent component, IComponentsCardModel owner)
    {
        component.Attach(owner);
    }

    private static void InvokeNoArg(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, System.Type.EmptyTypes)
                     ?? throw new InvalidOperationException(
                         $"Component {instance.GetType().FullName} missing {methodName}().");

        method.Invoke(instance, null);
    }
}