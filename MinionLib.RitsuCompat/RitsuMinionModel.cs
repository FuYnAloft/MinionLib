using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MinionLib.Minion;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace MinionLib.RitsuCompat;

public abstract class RitsuMinionModel : MinionModel,
    IModMonsterAssetOverrides,
    IModCreatureVisualsFactory,
    IModCreatureAnimatorFactory,
    IModCreatureCombatAnimationStateMachineFactory
{
    public virtual MonsterAssetProfile AssetProfile => MonsterAssetProfile.Empty;

    public virtual string? CustomVisualsPath => AssetProfile.VisualsScenePath;

    public virtual bool CustomHealthBarVisible => true;

    public override bool IsHealthBarVisible => CustomHealthBarVisible;

    protected override string VisualsPath => CustomVisualsPath ?? base.VisualsPath;

    NCreatureVisuals? IModCreatureVisualsFactory.TryCreateCreatureVisuals()
    {
        return TryCreateCreatureVisuals();
    }

    CreatureAnimator? IModCreatureAnimatorFactory.TryCreateCreatureAnimator(MegaSprite controller)
    {
        return SetupCustomCreatureAnimator(controller);
    }

    ModAnimStateMachine? IModCreatureCombatAnimationStateMachineFactory.TryCreateCombatAnimationStateMachine(
        Node visualsRoot)
    {
        return ResolveCombatAnimationStateMachine(visualsRoot);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        return SetupCustomCreatureAnimator(controller) ?? base.GenerateAnimator(controller);
    }

    protected virtual NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return null;
    }

    protected virtual CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        return null;
    }

    protected virtual ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(
        Node visualsRoot,
        MonsterModel monster)
    {
        return null;
    }

    private ModAnimStateMachine? ResolveCombatAnimationStateMachine(Node visualsRoot)
    {
        return SetupCustomCombatAnimationStateMachine(visualsRoot, this);
    }
}
