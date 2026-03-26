# 快速入门

欢迎使用 MinionLib！本指南将通过一个简单的示例，带你快速上手创建随从、相关卡牌和能力。我们将一步步实现以下功能：

1. 一个简单随从
2. 一张召唤随从的牌
3. 一个可点击触发的 Action
4. 一张可以强化随从的牌
5. 一张绑定了随从的牌

> 示例代码优先参考仓库现成文件：`Example/`。

## Part 1: 创建一个简单随从

要创建一个随从，只需要继承 `MinionModel`，并实现必要的属性和方法：

```csharp
public class MyMinion : MinionModel
{
	public override int MinInitialHp => 6; // 作为敌方方怪物生成时的血量，通常无需在意
	public override int MaxInitialHp => 6; // 作为敌方方怪物生成时的血量，通常无需在意
	protected override string VisualsPath => "res://Example/MinionTest/scenes/creature_visuals/pettest_attackaka.tscn"; // 随从的视觉资源路径，tscn 格式，建议参考原版游戏的怪物
    
    // 召唤时执行的代码，通常用来设置血量、应用初始能力等，options 是在召唤随从时传入的参数
	public override async Task OnSummon(Player owner, Creature self, MinionSummonOptions options) // 注意使用 self 而非 this
	{
		if (options.MaxHp is decimal maxHp)
			await CreatureCmd.SetMaxAndCurrentHp(self, maxHp); // 设置血量

		if (options.PrimaryStatAmount is decimal strength && strength > 0m)
			await PowerCmd.Apply<StrengthPower>(self, strength, owner.Creature, options.Source); // 根据传入的参数设置力量

		await PowerCmd.Apply<PetAttackerPower>(self, 1m, owner.Creature, options.Source); // 获得 “攻击者” 能力
	}
}
```
> [!NOTE]
> 随从的本质都是怪物（MonsterModel），所以可以像怪物一样做动画等等，也可以作为敌方怪物生成


## Part 2: 创建一张召唤随从的牌

随从需要通过卡牌等方式召唤，建议使用`MinionCmd.AddMinion`来处理召唤逻辑，以下是一个简单的示例，召唤一个 `MyMinion`：

```csharp
public sealed class SummonMyMinionCard : CustomCardModel
{
	public SummonMyMinionCard() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self) { } // 卡牌基础信息

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		_ = await MinionCmd.AddMinion<MyMinion>(Owner, new MinionSummonOptions(
			MaxHp: 8m,                              // 血量
			PrimaryStatAmount: 2m,                  // 主要参数（具体内容在随从的 OnSummon 里定义），还有次要参数等可以按需传入
			Source: this,                           // 召唤来源（通常是这张牌）
			Position: MinionPosition.Front));       // 站位（见后文，默认是前排）
	}

	protected override void OnUpgrade() { }
}
```


## Part 3: 给随从添加一个*行动*

*行动*（Action）是一种可以点击触发的能力，让随从（或玩家）拥有*行动*，它们/他们/她们就可以行动了。
只需要点击随从或者*行动*对应的能力图标，如果需要指定目标再指定目标，就可以执行了。

要创建一个*行动*，需要继承 `CustomActionModel`，并实现必要的属性和方法。

下面的示例实现了攻击这个*行动*，可以对一个敌人造成随从力量点伤害。

```csharp
public sealed class MyAttackAction : CustomActionModel
{
	public override TargetType TargetType => TargetType.AnyEnemy;           // 目标类型
	public override bool AutoRemoveAtTurnEnd => true;                       // 是否在回合结束自动移除
	public override PowerType Type => PowerType.Buff;                       // Power 的类型
	public override PowerStackType StackType => PowerStackType.Counter;     // Power 的堆叠属性
    
    // 核心重载，定义 Action 被触发时的行为，类似于卡牌的 OnPlay
    // 和卡牌一样，如果目标无需选定（如所有敌人），target 将会是 null
	protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature actor, Creature? target)
	{
		if (target == null) return;

		await MinionAnimCmd.PlayBumpAttackAsync(actor, target);                             // 播放撞击动画（在 MinionAnimCmd 中定义）
		await CreatureCmd.Damage(choiceContext, target, 4m, ValueProp.Move, actor, null);   // 造成伤害
	}
}
```

> [!NOTE]
> CustomActionModel 继承自 CustomPowerModel，所以它也可以像 Power 一样被应用到随从身上，显示在状态栏里。
> 因为使用的是 CustomPowerModel，指定图像资源的方式也相同


## Part 4: 创建一张强化随从的牌

想把随从选为目标？你需要 MinionLib 的自定义目标类型系统。

MinionLib 实现了一个可以简单使用的自定义目标类型系统，可以自定义新的 TargetType，像使用原版的 TargetType 一样作为相关参数。

MinionLib 还提供了许多预定义的目标类型，位于 `MinionLib.Targeting.MinionTargetTypes`中。

```csharp
public sealed class EmpowerMinionCard : CustomCardModel
{
	public EmpowerMinionCard() : base(1, CardType.Skill, CardRarity.Uncommon, 
        MinionTargetTypes.AnyMinion // 使用了 MinionTargetTypes.AnyMinion 这个预定义的目标类型，表示可以选择任何一个（你的）随从作为目标
        ) { }

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Target is not { Monster: MinionModel } target) return;

		await PowerCmd.Apply<StrengthPower>(target, 2m, Owner.Creature, this);
		await PowerCmd.Apply<DexterityPower>(target, 2m, Owner.Creature, this);
	}
}
```

> [!NOTE]
> 关于如何创建自定义目标类型，参见 // TODO

## Part 5: 创建一张绑定了随从的牌

想要生成一张绑定了随从的牌，可以按照随从的状态产生不同的效果？没问题！

要创建绑定了随从的牌，继承 CustomMinionBoundCardModel，

```csharp
public sealed class BoundStrikeCard()
        : CustomMinionBoundCardModel(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    // 如果绑定的随从已死亡，框变为红色
    protected override bool ShouldGlowRedInternal => ResolveBoundMinion() is not { IsAlive: true };
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BoundMinionDamageVar(0m, ValueProp.Move)]; // 定义 DynamicVar，与原版 DamageVar 不同的是，此处会依据随从的数值计算

    
	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Target == null) return;
		var minion = ResolveBoundMinion();                                // 解析绑定的随从
		if (minion is not { IsAlive: true }) return;

        await MinionAnimCmd.PlayBumpAttackAsync(minion, cardPlay.Target); // 播放撞击动画
        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars["BoundPetDamage"].BaseValue,
            ValueProp.Move, minion, this);                                // 造成伤害，方法和原版类似
	}
}
```

此类卡牌需要在创建时进行绑定操作，比如在一个能力中给予：

```csharp
public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
    CombatState combatState)
{
    if (side != Owner.Side || !Owner.IsAlive || Owner.PetOwner == null) return;
    for (var i = 0; i < Amount; i++)
    {
        var petOwner = Owner.PetOwner;                                          // 解析随从的主人
        var card = combatState.CreateCard<BoundStrikeCard>(petOwner);           // 创建卡牌，拥有者为随从的主人
        card.BoundMinionCombatId = Owner.CombatId;                              // 绑定随从的 CombatId，卡牌会通过这个 Id 来解析绑定的随从
        card.BoundMinionNameSnapshot = Owner.Name;                              // 绑定随从的名字快照，推荐设置以为卡牌描述显示提供后备
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, false);
    }
}
```

在卡牌描述本地化中，可以使用 `{BoundMinionName}` 表示随从的名字，例如：

```json
{
  "BOUND_STRIKE_CARD.title": "随从突击",
  "BOUND_STRIKE_CARD.description": "让随从[gold]{BoundMinionName}[/gold]对1名敌人造成{BoundPetDamage:diff()}点伤害。"
}
```

> [!NOTE]
> 这里的 `BoundMinionDamageVar` 是一个在此库中预定义的 DynamicVar。要创建自己的 DynamicVar，参见 // TODO
> （这其实应该算原版游戏模组的内容）

## 能力：守护者（Guardian）

本库模组定义了一个能力——守护者（Guardian）。拥有这个能力的随从可以 `挡在你的前方，为你阻挡伤害`。

只需要使用 `PowerCmd.Apply<MinionGuardianPower>(...)` 就可以给予随从这个能力了：

## 补充材料

此处会简单说明一些常用的进阶功能。你可以前往对应的详细页面

### 随从站位系统

本模组定义了5种随从站位：
```csharp
public enum MinionPosition
{
    Front,
    Back,
    FrontUpper,
    BackUpper,
    Upper
}
```
站位会影响视觉位置，也会影响相关机制。

### 自定义目标系统

本库提供了一个自定义目标系统，可以让你定义新的 TargetType 来满足不同的玩法需求。

只需要写几个“谓词”（一种函数，输入是 Creature 等，输出是 bool 表示这个 Creature 是否满足条件）就可以定义一个新的目标类型了。

然后在 `CustomTargetTypeManager` 中注册这个新的目标类型，就可以在卡牌和能力中使用了。

### 随从位移

想要移动随从的位置？没问题！本库提供了一种特别的方法，可以相对安全地修改 pets 的顺序。