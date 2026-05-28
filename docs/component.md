# Component 使用文档

## 简介

本文面向已经熟悉 C# 和 STS2 `CardModel` 的开发者。目标是把 MinionLib Component 的使用边界、推荐写法、常见实现路径整理成一份可以直接交给开发者或代码生成工具使用的说明。

Component 的核心价值在于把“卡牌身份”和“可复用行为”拆开管理。一张卡仍然声明费用、类型、稀有度、目标、数值和关键词；可复用的行为进入 Component，例如打出时追加伤害、右键融合、根据能量改写费用、跨时机记录战斗历史、向卡牌描述追加标准文本。

### 适用场景

Component 适合处理这些真实开发场景：

- 多张卡有同一段行为：三张技能牌都写“打出时抽 1 张牌”，开发者继续复制 `OnPlay` 会让后续升级、描述和测试散在多个类里。一个 `DrawOnPlayComponent` 可以统一处理出牌时机、数值展示和合并规则。
- 机制带有可保存状态：融合牌需要记录本回合是否融合、融合过几张牌、融合了哪些卡。状态留在卡牌类里时，复制、存档和反序列化容易漏字段。Component 可以用 `[ComponentState]` 声明需要进入组件状态 blob 的字段。
- 行为发生在卡牌主流程之外：瞬念召唤、回合开始、卡牌进入战斗、消耗后触发这类逻辑，直接堆到卡牌类里会把主效果和监听逻辑混在一起。`TimingCardComponent` 可以声明监听的时机，并把上下文收束到一个组件类里。
- 卡牌显示需要和行为同步：费用改写、高亮、额外目标、无法使用、标准前缀文本、HoverTip 都可以由 Component 提供。开发者看到 `CanonicalComponents` 就能知道这张卡额外挂了哪些规则。
- MR 评审需要减少重复检查：同一个机制散落在十几张卡的 `OnPlay` 里时，每个 MR 都要重新检查费用判断、选择 UI、同步、描述和升级差值。机制收进 Component 后，评审者重点看组件实现一次，之后在卡牌 MR 里确认参数、挂载位置和独有结算即可。
- AI 代码生成需要复用路径：让 AI 直接从卡牌文本生成完整 `OnPlay`，它容易重复写费用判断、选择逻辑和状态字段。文档明确“已有机制先挂 Component，卡牌只补参数和独有效果”，可以降低单次生成的复杂度，也能引导 AI 复用已经测试过的组件。

### 使用边界

Component 不适合承载一次性的大段卡牌主效果。某张卡独有的复杂结算继续放在卡牌自己的 `OnPlay(..., ComponentContext)` 里；Component 负责可复用、可组合、需要进入通用流程的部分。

## Quickstart：完整范例

本节先给出三种最常见落地方式：让卡牌基类接入 `ComponentsCardModel`、写一个普通 `CardComponent`、写一个跨时机监听的 `TimingCardComponent`。后续章节会解释这些代码背后的流程、序列化和本地化规则。

### 场景 1：接入 ComponentsCardModel

先让项目自己的卡牌基类继承 `ComponentsCardModel`，业务卡牌继续继承这个项目基类。这样每张卡都能声明 `CanonicalComponents`，并且所有卡牌 hook 都使用带 `ComponentContext` 的签名。

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using YourMod.Components;

namespace YourMod.Cards;

public abstract class MyCardBase(
    int cost,
    CardType type,
    CardRarity rarity,
    TargetType targetType)
    : ComponentsCardModel(cost, type, rarity, targetType);

public sealed class QuickStudy : MyCardBase
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new DrawOnPlayComponent(1),
        new SameOwnerPlayCounterComponent()
    ];

    public QuickStudy()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        // 当前卡独有的结算写在 Core 阶段。
        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddComponent(new DrawOnPlayComponent(1), isUpgrade: true);
    }
}
```

这张卡启动时挂上两个组件：

- `DrawOnPlayComponent(1)`：打出后抽 1 张牌，升级时通过 `AddComponent(..., isUpgrade: true)` 追加到 2。
- `SameOwnerPlayCounterComponent()`：监听战斗开始、打牌后、战斗结束，并记录当前拥有者本场战斗打出了多少张牌。

### 场景 2：普通 CardComponent

普通组件适合复用一段固定行为。下面的 `DrawOnPlayComponent` 负责“打出后抽 N 张牌”，同时处理状态保存、描述数值、升级合并。

```csharp
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;

namespace YourMod.Components;

public sealed partial class DrawOnPlayComponent : CardComponent
{
    [ComponentState<DynamicVar>]
    public partial int Cards { get; private set; }

    public DrawOnPlayComponent(int cards)
    {
        Cards = cards;
    }

    public override Task OnPlayPostfix(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return CardPileCmd.Draw(choiceContext, Cards, Card!.Owner);
    }

    public override bool TryMergeWith(
        ICardComponent incoming,
        ApplyComponentOptions options,
        out ICardComponent? merged)
    {
        if (incoming is not DrawOnPlayComponent draw)
        {
            merged = null;
            return false;
        }

        Cards += draw.Cards;
        if (options.IsUpgrade)
            DynamicVars["Cards"].SetWasJustUpgraded();

        merged = this;
        return true;
    }
}
```

本地化：

```yaml
DrawOnPlayComponent:
  prefix: 打出时抽{Cards:diff()}张牌。
```

### 场景 3：TimingCardComponent

`TimingCardComponent` 适合监听卡牌主流程之外的事件。下面的组件记录当前卡牌拥有者本场战斗打出了多少张牌，并在战斗开始和战斗结束时重置。

```csharp
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using MinionLib.Component.Utils;

namespace YourMod.Components;

public sealed partial class SameOwnerPlayCounterComponent : TimingCardComponent
{
    [ComponentState<DynamicVar>]
    public partial int PlayedThisCombat { get; private set; }

    public SameOwnerPlayCounterComponent()
        : base(Timing.BeforeCombatStart, Timing.AfterCardPlayed, Timing.AfterCombatEnd) { }

    protected override Task OnTimingPostfix(OnTimingContext context)
    {
        if (Card?.Owner == null) return Task.CompletedTask;

        switch (context.Timing)
        {
            case Timing.BeforeCombatStart:
            case Timing.AfterCombatEnd:
                PlayedThisCombat = 0;
                break;
            case Timing.AfterCardPlayed:
                var playedCard = context.CardPlay?.Card;
                if (playedCard?.Owner == Card.Owner)
                    PlayedThisCombat++;
                break;
        }

        return Task.CompletedTask;
    }
}
```

本地化：

```yaml
SameOwnerPlayCounterComponent:
  postfix: 本场战斗你已打出{PlayedThisCombat}张牌。
```

## 核心模型与原则

### 基本模型

MinionLib 提供两个核心类型：

- `ComponentsCardModel`：带组件列表的卡牌基类，继承自 `CardModel`。
- `CardComponent`：组件基类，提供状态、描述、HoverTip、目标、费用、出牌时机、右键等扩展点。

项目可以让自己的业务卡牌基类直接或间接继承 `ComponentsCardModel`。这样普通卡牌只需要继承业务基类，就天然支持 `CanonicalComponents`、`AddComponent`、`GetComponent<T>` 和组件化 hook。

### ComponentsCardModel 标准原则

使用 `ComponentsCardModel` 后，卡牌类遵守下面几条原则：

- 所有卡牌 hook 都使用带 `ComponentContext componentContext` 的签名。例如 `OnPlay(PlayerChoiceContext, CardPlay, ComponentContext)`、`AfterCardPlayed(PlayerChoiceContext, CardPlay, ComponentContext)`、`OnUpgrade(ComponentContext)`、`AfterDowngraded(ComponentContext)`。
- 旧的原版 hook 会被 sealed。编译器提示 “Try adding `ComponentContext componentContext` as the last parameter” 时，按提示改签名。
- 卡牌自己的主效果写在 Core 阶段。组件可以在 Prefix 阶段先执行，也可以在 Postfix 阶段收尾。
- `CanonicalComponents` 声明初始组件。这里应该放构造参数稳定、可序列化、可深拷贝的组件。
- `AddComponent` 适合运行时授予行为。运行时挂载的组件同样需要能序列化和反序列化，尤其是跨回合、进存档、复制卡牌后仍可能存在的组件。
- `GetComponent<T>` 用来调用组件提供的显式入口，例如 `this.UseMode(...)` 内部通常会拿到 `ModeComponent` 再执行选择流程。

Quickstart 里的 `MyCardBase` 和 `QuickStudy` 展示了最小接入路径。`CanonicalComponents` 表示卡牌的初始组件。卡牌第一次访问组件列表时，MinionLib 会深拷贝这些组件并挂到当前卡牌实例上。复制卡、存档恢复和战斗中生成卡时，组件状态会跟着当前卡实例走。

运行时追加组件：

```csharp
card.AddComponent(new DrawOnPlayComponent(1));
```

运行时读取组件：

```csharp
var mode = card.GetComponent<ModeComponent>();
```

同类型组件默认会尝试合并。需要控制合并规则时，在组件里覆写 `TryMergeWith` 和 `TrySubtractiveMergeWith`。

### 推荐写法

组件类建议采用 Quickstart 中 `DrawOnPlayComponent` 的形态：`sealed partial` 类型、可序列化状态字段、明确的 hook、可评审的合并规则。

推荐规则：

- 组件类使用 `sealed partial`。MinionLib 生成器会生成注册、状态序列化、DynamicVar 绑定等代码；`partial` 是硬要求，`sealed` 可以避免继承层级把生成规则搞复杂。
- 需要保存的字段加 `[ComponentState]`。字段参与描述数值展示时，优先使用 `[ComponentState<TDynamicVar>] public partial ...`。
- 战斗行为继续走游戏命令，例如 `CardPileCmd`、`DamageCmd`、`CreatureCmd`、`PowerCmd`。组件只决定何时执行、执行哪些参数。
- 卡牌主效果覆写带 `ComponentContext` 的签名：

```csharp
protected override Task OnPlay(
    PlayerChoiceContext choiceContext,
    CardPlay cardPlay,
    ComponentContext componentContext)
{
    ...
}
```

- 升级逻辑覆写：

```csharp
protected override void OnUpgrade(ComponentContext componentContext)
{
    AddComponent(new StrikeComponent(3), isUpgrade: true);
}
```

这些签名属于 `ComponentsCardModel` 的标准原则。卡牌和组件同时存在时，优先让组件承担可复用行为，让卡牌类保留当前卡独有的结算。

### 出牌流程

`ComponentsCardModel` 把一次卡牌 hook 拆成多个阶段：

1. 按组件声明顺序执行 `OnPlayPrefix`。
2. 执行卡牌自己的 `OnPlay(..., ComponentContext)`。
3. 按组件声明逆序执行 `OnPlayPostfix`。

这个顺序适合表达“组件包住卡牌主效果”的结构。比如：

- `StrikeComponent` 放在 Prefix：卡牌主效果前先造成伤害。
- `DrawOnPlayComponent` 放在 Postfix：卡牌主效果结束后抽牌。
- 模式类组件可以在卡牌主效果中被显式调用，让卡牌自己决定模式结算前后还要做什么。

组件 hook 收到的 `ComponentContext` 带有当前阶段。高级组件可以移动阶段来跳过后续流程，但普通组件不建议修改阶段。

### 状态与序列化

卡牌保存组件状态时，会写入两个层次的信息：

- 组件列表：每个组件的 `ComponentId`。
- 组件字段：每个组件中标了 `[ComponentState]` 的属性。

`ComponentId` 默认由生成器根据根命名空间和类名生成。改组件类名、改根命名空间、移动组件到另一个程序集，都会影响反序列化。已经发布到玩家存档里的组件，后续重命名需要迁移策略。

组件字段建议使用生成器支持的简单类型：`int`、`decimal`、`bool`、`string`、枚举、数组、`List<T>` 和其它可序列化对象。复杂对象可以走 JSON fallback，但评审时要检查版本兼容和空值处理。

Note：当前 Component 设计默认组件可以被序列化、反序列化和深拷贝。挂在 `CanonicalComponents` 里的组件会进入卡牌复制和存档流程；通过 `AddComponent` 在战斗中挂上的组件也可能在复制卡、保存战斗、同步状态时被序列化。组件里不要保存 `CardModel`、`Creature`、`PlayerChoiceContext`、委托实例、UI 节点这类运行时对象。必须引用运行时逻辑时，保存稳定 id、类型名、方法名或枚举值，再在 `OnAttach`、hook 执行时重新解析。

运行时挂载函数需要额外检查生命周期：

- 挂上的组件只在当前动作内有效：优先在动作结束前移除，或直接在当前 `OnPlay` 中执行。
- 挂上的组件跨回合存在：状态字段必须覆盖恢复所需信息。
- 挂上的组件保存了静态回调 id：反序列化后要能从注册表或反射重新找到回调。
- 挂上的组件依赖目标卡当前属性：复制卡后应通过 `Card` 重新读取，不应缓存旧卡引用。

`[ComponentState<TDynamicVar>]` 会做两件事：

- 该属性进入组件状态。
- 生成一个同名 `DynamicVar`，可在本地化文本里用 `{PropertyName}`、`{PropertyName:diff()}` 等格式展示。

示例：

```csharp
[ComponentState<DamageVar>(DamageProps.card)]
public partial int Damage { get; private set; }
```

组件内部修改这个属性时，生成器会同步更新 `DynamicVars["Damage"].BaseValue`。升级时如果需要卡面显示绿色差值，调用：

```csharp
DynamicVars.Damage.SetWasJustUpgraded();
```

### 描述文本

`CardComponent` 默认读取两个本地化 key：

- `cards.<ComponentId>.prefix`
- `cards.<ComponentId>.postfix`

组件卡牌描述会自动收集所有组件的 prefix 和 postfix，并注入到卡牌描述的组件占位中。本节先说明 Component 自身的本地化 key 约定，后面的 Keyword 小节再以 BaseMod 的关键词注册流程为例。

如果项目使用 YAML 写本地化，下面的 YAML 形态依赖 `FuYnAloft/YAML-Loc-Sts2` 或等价转换工具。没有这个依赖时，需要直接写最终游戏读取的 JSON 或本地化表。

示例：

```yaml
DrawOnPlayComponent:
  prefix: 打出时抽{Cards:diff()}张牌。
```

对应组件：

```csharp
public sealed partial class DrawOnPlayComponent : CardComponent
{
    [ComponentState<DynamicVar>]
    public partial int Cards { get; private set; }
}
```

如果组件需要把额外对象塞进描述，例如 Power 名称：

```csharp
[LocArg]
private LocString PowerTitle => new("powers", $"{IdEntry}.title");
```

如果组件需要拼接另一段本地化文本，可以覆写 `FormatPrefix`：

```csharp
protected override string FormatPrefix(LocString loc)
{
    loc.Add("Content", new LocString("cards", ResolveContentKey()));
    return loc.GetFormattedText();
}
```

写描述时要让文本还原到玩家操作：  
“右键这张牌，选择至多 2 张手牌融合；被选择的牌从战斗中移除。”比“进行融合处理”更容易评审。

### HoverTip、目标、费用和可用性

Component 可以影响多个卡牌表面属性：

```csharp
public override IEnumerable<IHoverTip> HoverTips =>
[
    HoverTipFactory.FromKeyword(ModKeywords.Fusion)
];

public override TargetType? ExtraTargetType => TargetType.AnyEnemy;

public override bool IsPlayable => false;

public override Color? GlowColor => IsActive ? Colors.Gold : null;
```

常见用途：

- `HoverTips`：补充 Keyword、Power、选项卡预览。
- `ExtraTargetType`：组件给原本无目标的卡追加目标需求。
- `IsPlayable`：无法使用、等待条件满足、只允许右键操作的卡。
- `GlowColor` / `ShouldGlowGoldInternal`：卡牌满足额外条件时给玩家反馈。
- `TryModifyEnergyCostInCombat`：根据当前能量、手牌状态或组件状态改写费用。

费用改写组件要特别小心递归。组件读取 `card.EnergyCost.GetResolved()` 时，如果自己也参与费用修改，需要一个 suppression scope 或等价保护，避免刷新状态时再次触发费用计算。

## Keyword 与本地化

### Keyword 与 Component 的职责

`CardKeyword` 是卡面的字段和 tooltip 入口。它让玩家看到“融合”“爆能强化”“无法使用”等关键词，也让代码可以通过 `card.Keywords.Contains(...)` 做轻量判定。

Component 负责行为。它决定右键能不能触发、触发后选哪些牌、费用怎样变化、出牌时执行哪些命令、状态如何保存。

一个完整机制通常包含三层：

- Keyword：展示字段和 tooltip。
- Component：执行可复用行为。
- 卡牌类：声明自己拥有该机制，并处理独有结算。

以“模式选择”类机制为例，卡牌类通常会声明：

```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords =>
[
    ModKeywords.Mode
];

protected override IEnumerable<ICardComponent> CanonicalComponents =>
[
    new ModeComponent([
        ModeOption.Option<OptionA>(ResolveA),
        ModeOption.Option<OptionB>(ResolveB)
    ])
];

protected override Task OnPlay(
    PlayerChoiceContext choiceContext,
    CardPlay cardPlay,
    ComponentContext componentContext)
{
    return this.UseMode(choiceContext, cardPlay, componentContext);
}
```

评审这类卡时，需要同时检查三件事：

- `CanonicalKeywords` 是否让玩家看到机制。
- `CanonicalComponents` 是否真的挂了机制行为。
- `OnPlay` 是否调用了组件提供的入口，或者组件自身是否在 Prefix/Postfix 中执行。

只加 Keyword 会出现“卡面有字，打出没效果”。只加 Component 会出现“效果存在，卡面缺少关键词和 tooltip”。

### 最小自定义 Keyword

Keyword 注册属于具体内容库的能力，Component 本身不依赖这部分。下面以 BaseMod 风格的关键词注册流程为例，目标是得到一个静态 `CardKeyword` 字段，并让它能从 `card_keywords` 表读取标题和说明。

下面实现一个最小自定义 Keyword：`Overload`。

1. 定义静态字段。

```csharp
using BaseMod.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace YourMod.Cards;

public static class ModKeywords
{
    [CustomEnum("OVERLOAD")]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Overload;
}
```

`CustomEnum("OVERLOAD")` 决定生成的关键词后缀。BaseMod 会按项目的注册规则生成完整本地化前缀，例如 `YOURMOD-OVERLOAD`。不同内容库的 attribute 名称可能不同，关键点是最终要得到稳定的 `CardKeyword` 值和稳定的本地化 key。

`KeywordProperties` 控制关键词标题是否自动进入卡牌描述：

- `AutoKeywordPosition.None`：只注册关键词和 tooltip，描述由卡牌或组件自己写。
- `AutoKeywordPosition.Before`：把关键词标题放到描述前。
- `AutoKeywordPosition.After`：把关键词标题放到描述后。

2. 添加本地化。

如果项目使用 `FuYnAloft/YAML-Loc-Sts2`，可以写成：

```yaml
Overload:
  title: 过载
  description: 本回合额外支付能量后发动的能力。
```

如果没有使用 `FuYnAloft/YAML-Loc-Sts2`，直接写 JSON 或其它本地化表，最终 key 需要落到 `card_keywords` 表：

```json
{
  "YOURMOD-OVERLOAD.title": "过载",
  "YOURMOD-OVERLOAD.description": "本回合额外支付能量后发动的能力。"
}
```

3. 在卡牌上声明。

```csharp
public override IEnumerable<CardKeyword> CanonicalKeywords =>
[
    ModKeywords.Overload
];
```

4. 需要手动 HoverTip 时添加。

```csharp
protected override IEnumerable<IHoverTip> ExtraHoverTipsC =>
[
    HoverTipFactory.FromKeyword(ModKeywords.Overload)
];
```

多数卡牌只需要第 3 步。`HoverTipFactory.FromKeyword(...)` 适合机制说明没有自动出现在当前位置、或某个 Power/Relic 也需要展示同一个 tooltip 的场景。

## 常用实现场景

### 普通 Component 检查点

Quickstart 的 `DrawOnPlayComponent` 是最小普通组件范例。写同类组件时，按下面顺序检查即可：

这个组件解决了四个点：

- 行为：出牌后抽牌。
- 描述：组件自动追加“打出时抽 N 张牌”。
- 状态：`Cards` 随卡保存和复制。
- 升级：升级追加同类组件时，`TryMergeWith` 把数值合并，并标记升级差值。

### 运行时授予组件

运行时授予组件适合表达“给另一张牌临时附加行为”。例如某张 Power 让手牌中的下一张攻击牌获得“打出时抽 1 张牌”：

```csharp
targetCard.AddComponent(new DrawOnPlayComponent(1));
```

评审时需要检查：

- 目标卡是否是 `IComponentsCardModel`，否则无法挂组件。
- 授予的是永久到本场战斗结束、直到打出、直到回合结束，还是进入存档后也保留。
- 同类组件合并后数值是否符合预期。
- 如果效果需要移除，是否有对应 `SubtractComponent` 或 `RemoveComponent<T>`。

对于只影响单次出牌的短效行为，直接在当前卡的 `OnPlay` 中执行更清晰。运行时授予适合玩家之后能看到、能交互、能被复制或能被保存的行为。

### 右键组件

右键组件适合处理“这张牌在手牌中可以额外操作”的机制。典型流程：

1. `CanHandleRightClickLocal` 控制本地 UI 是否显示可右键。
2. `CanHandleRightClick` 做权威判定。
3. `OnRightClick` 打开选择 UI、执行命令、写入组件状态。

示例结构：

```csharp
public abstract partial class FusionComponent : CardComponent
{
    [ComponentState]
    public bool HasFusedThisTurn { get; protected set; }

    [ComponentState<DynamicVar>]
    public partial byte MaxCardsToFuse { get; protected set; }

    public override bool CanHandleRightClick(RightClickContext context)
    {
        return Card?.Pile?.Type == PileType.Hand
               && !HasFusedThisTurn
               && HasAnyValidFusionTarget();
    }

    public override async Task OnRightClick(
        PlayerChoiceContext choiceContext,
        RightClickContext clickContext)
    {
        var selected = await SelectFusionCards(choiceContext);
        if (selected.Count == 0) return;

        await CardPileCmd.RemoveFromCombat(selected);
        HasFusedThisTurn = true;
        await OnFuse(selected);
    }
}
```

右键组件里尤其需要避免只改本地 UI 状态。选择、移除卡、加牌、改费用这类动作应走命令或已有同步 API。

### TimingCardComponent

`TimingCardComponent` 适合跨时机监听。组件声明一组 `Timing`，MinionLib 生成的 hook 会在对应时间调用 `OnTimingPrefix` 或 `OnTimingPostfix`。Quickstart 的 `SameOwnerPlayCounterComponent` 展示了战斗开始重置、打牌后记录、战斗结束清理的完整形态。

写这类组件时要明确三件事：

- 监听的是谁的事件：当前卡的拥有者、所有玩家、所有怪物，还是当前战斗全局。
- 组件状态在哪里保存：卡上、玩家全局状态、战斗历史，还是静态缓存。
- 触发动作如何同步：自动打出、抽牌、消耗、选择卡牌都需要使用游戏命令或同步选择 API。

## 落地检查

### 常见错误

- 只加 Keyword，忘记挂 Component。卡面显示机制，实际出牌时没有额外行为。
- 只挂 Component，忘记加 Keyword 或 HoverTip。行为存在，玩家看不出这张牌为什么能右键、为什么改了费用。
- 组件没有 `partial`。生成器无法生成注册和状态序列化代码。
- `[ComponentState<TDynamicVar>]` 属性没有写成 `partial`。属性值可以保存，但运行时修改后 DynamicVar 不会自动同步。
- 构造函数捕获运行时对象。组件需要能深拷贝和反序列化，构造参数应优先使用数字、字符串、类型 id、静态方法 id。
- 在组件里直接改集合或血量。战斗动作应优先使用命令系统，让历史、同步和 UI 更新保持一致。
- 组件合并规则没写。升级或运行时多次 `AddComponent` 后，开发者预期是叠加，实际可能追加多个实例或合并失败。
- 本地化 key 跟 `ComponentId` 对不上。卡牌描述里组件文本为空，评审时需要看最终生成的本地化表。

### 评审清单

评审一个新 Component 或使用 Component 的卡牌时，按这个顺序看：

1. 卡牌是否继承了支持组件的基类。
2. `CanonicalComponents` 是否声明了机制行为。
3. `CanonicalKeywords` 是否覆盖玩家需要看到的关键词。
4. 卡牌是否覆写带 `ComponentContext` 的 hook。
5. 组件类是否 `sealed partial`。
6. 需要保存的状态是否标了 `[ComponentState]`。
7. 需要展示的数值是否使用 `[ComponentState<TDynamicVar>]` 或 `[LocArg]`。
8. 出牌、抽牌、伤害、消耗、加牌是否走命令 API。
9. 同类型组件多次出现时，合并规则是否符合设计。
10. 组件文案、Keyword 文案和 HoverTip 是否都能在游戏中看到。
11. 复制卡、升级预览、存档恢复、战斗结束清理是否覆盖。
12. CI 失败时先看生成器报错、本地化缺 key、组件构造参数和旧 hook 签名。

### 推荐文件位置

一个中等规模项目可以按机制归档：

```text
Cards/
  CardKeywords/
    ModKeywords.cs
  CardBase/
    MyCardBase.cs
Components/
  Common/
    StrikeComponent.cs
    DrawOnPlayComponent.cs
  Modes/
    ModeComponent.cs
    ModeOption.cs
  Fusion/
    FusionComponent.cs
Localization/
  card_keywords.yaml
  components.yaml
```

组件和关键词分开放，可以让开发者从卡牌类快速跳到行为实现。MR 中看到一张卡新增 `ModKeywords.Fusion` 和 `new FusionComponent(...)` 时，评审者能立刻检查显示层和行为层是否同步。

### 参考实现

读源码时建议从这些文件开始，路径以 MinionLib 仓库根目录为基准：

- `MinionLib/Component/CardComponent.cs`
- `MinionLib/Component/ComponentsCardModel.cs`
- `MinionLib/Component/Partials/ComponentsCardModel_Hooks.cs`
- `MinionLib/Component/Partials/ComponentsCardModel_Modifiers.cs`
- `MinionLib/Component/Utils/TimingCardComponent.cs`
- `MinionLib/Component/Core/CardComponentStateSerializer.cs`
- `MinionLib.Generators/CardComponentRegisterSourceGenerator.cs`
- `MinionLib.Generators/ComponentStatePropertyGenerator.cs`
- `MinionLib.Generators/DynamicVarSourceGenerator.cs`

示例项目中的 `Scripts/Components/` 也有可直接对照的组件形态：普通出牌组件、费用改写组件、右键组件、多选项组件和跨时机组件都能在这里找到。
