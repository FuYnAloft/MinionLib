using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MinionLib.Component.Extensions;

namespace MinionLib.Component.Patches;

[HarmonyPatch(typeof(NetFullCombatState), nameof(NetFullCombatState.ToString))]
public class NetFullCombatStateComponentsLogPatch
{
    public static void AppendComponentInfo(StringBuilder sb, NetFullCombatState.CardState card)
    {
        sb.Append(card.card.GetComponentsLogString(2, "\t"));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var matcher = new CodeMatcher(instructions, il);

        // --- 步骤 A：找到卡牌局部变量 (card) 的索引 ---
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Call,
                AccessTools.PropertyGetter(typeof(List<NetFullCombatState.CardState>.Enumerator), "Current"))
        );

        if (!matcher.IsValid)
            throw new Exception("Transpiler 失败: 找不到 Enumerator.Current");

        matcher.Advance(1); // 前进 1 步，来到保存卡牌的局部变量 (stloc.s card)
        var cardLocalOperand = matcher.Operand; // 抓取变量操作数 (比如 V_13 等)
        var loadCardOpcode = matcher.Opcode == OpCodes.Stloc_S ? OpCodes.Ldloc_S : OpCodes.Ldloc;

        // --- 步骤 B：找到循环结尾的 MoveNext() ---
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Call,
                AccessTools.Method(typeof(List<NetFullCombatState.CardState>.Enumerator), "MoveNext"))
        );

        if (!matcher.IsValid)
            throw new Exception("Transpiler 失败: 找不到 Enumerator.MoveNext");

        // 后退 1 步，来到 MoveNext 之前的汇聚点 (ldloca.s V_12)
        matcher.Advance(-1);

        var loopCondPos = matcher.Pos; // 记录当前指令位置
        var loopCondInst = matcher.Instruction;

        // --- 步骤 C：处理复杂的 Label 分流机制 ---
        // 1. 把所有跳到此处的 Label 全部抽离出来（这其中混杂着内部 if 跳转和外部初始跳转）
        var extractedLabels = new List<Label>(loopCondInst.labels);
        loopCondInst.labels.Clear();

        // 2. 为 MoveNext 生成一个全新的、干净的 Label
        var newLoopCondLabel = il.DefineLabel();
        loopCondInst.labels.Add(newLoopCondLabel);

        // 3. 往回找循环刚开始时的那个初始 br 跳转
        matcher.MatchStartBackwards(
            new CodeMatch(i => (i.opcode == OpCodes.Br || i.opcode == OpCodes.Br_S) &&
                               i.operand is Label lbl && extractedLabels.Contains(lbl))
        );

        if (matcher.IsValid)
        {
            // 将初始跳转的终点指向新 Label，让它直接跳过我们的自定义代码
            matcher.Instruction.operand = newLoopCondLabel;
        }

        // --- 步骤 D：回到循环尾部并插入我们的代码 ---
        matcher.Advance(loopCondPos - matcher.Pos); // 精准跳回刚才的 loopCondPos

        var injection = new List<CodeInstruction>
        {
            // 压入参数 1: 最外层的 stringBuilder1 (局部变量 0)
            new (OpCodes.Ldloc_0),

            // 压入参数 2: card 结构体
            new (loadCardOpcode, cardLocalOperand),

            // 调用我们的静态 C# 方法
            new (OpCodes.Call,
                AccessTools.Method(typeof(NetFullCombatStateComponentsLogPatch), nameof(AppendComponentInfo)))
        };

        // 将之前剥离的 Label（原本挂在 MoveNext 上的）贴给注入代码的第一行！
        // 这样一来，所有卡牌属性的内部 if 判断如果跳过，就会跳向我们的追加代码！
        injection[0].labels.AddRange(extractedLabels);

        // 正式插入
        matcher.InsertAndAdvance(injection);

        return matcher.InstructionEnumeration();
    }
}
