using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MinionLib.Minion;

namespace MinionLib.BaseLibCompat;

public abstract class BaseLibMinionModel : MinionModel, ICustomModel, ISceneConversions
{
    public virtual string? CustomVisualPath => null;

    public virtual bool CustomHealthBarVisible => true;

    public override bool IsHealthBarVisible => CustomHealthBarVisible;

    protected override string VisualsPath => CustomVisualPath ?? base.VisualsPath;

    public virtual NCreatureVisuals? CreateCustomVisuals()
    {
        return null;
    }

    public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        return null;
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        return SetupCustomAnimationStates(controller) ?? base.GenerateAnimator(controller);
    }

    public void RegisterSceneConversions()
    {
        CustomVisualPath?.RegisterSceneForConversion<NCreatureVisuals>();
    }
}
