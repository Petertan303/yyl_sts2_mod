using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Patches;

/// <summary>
///     太极重定向补丁 (单体): 给 <see cref="CreatureCmd.Damage" /> 的全部"单体目标"重载
///     加 Harmony 前缀 —— 当目标是你、且你身上有 <see cref="TaiChiMark" /> 时,
///     把目标改写为一名随机敌人 (你不受伤害, 敌人替你挨打)。
///     <para>
///         说明:
///         - 只改 target 引用, 伤害数值/来源/动画管线全部原样, 因此格挡、易伤、
///           无实体等防御结算对"替身"自然生效。
///         - 幂等: 重定向后的 target 已不是玩家, 其他重载的前缀不会二次触发。
///         - [2026-09-20] 实测敌方 AI 攻击并不走单体重载 (补丁 OK 但从未触发) ——
///           主路径是 <see cref="TaiChiRedirectMultiPatch" /> 的多目标重载 + 兜底的
///           <see cref="TaiChiMark.AfterDamageReceived" /> 事件转移 (倒映式)。
///     </para>
/// </summary>
[HarmonyPatch]
internal static class TaiChiRedirectPatch
{
    private static readonly Type[][] OverloadSigs =
    {
        // (ctx, target, amount, props, dealer, cardSource, cardPlay)
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay) },
        // (ctx, target, amount, props, dealer)
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature) },
        // (ctx, target, amount, props, cardSource, cardPlay)
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel), typeof(CardPlay) },
        // (ctx, target, damageVar, dealer, cardSource, cardPlay)
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(DamageVar), typeof(Creature), typeof(CardModel), typeof(CardPlay) },
        // (ctx, target, damageVar, dealer)
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(DamageVar), typeof(Creature) },
        // (ctx, target, damageVar, cardSource, cardPlay)
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(DamageVar), typeof(CardModel), typeof(CardPlay) },
    };

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var sig in OverloadSigs)
        {
            var method = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), sig);
            if (method != null)
                yield return method;
        }
    }

    private static void Prefix(ref Creature target)
    {
        if (target == null || !target.IsPlayer) return;
        var combatState = target.CombatState;
        if (combatState == null) return;

        var mark = target.GetPower<TaiChiMark>();
        if (mark == null || mark.Amount <= 0) return;

        var candidates = combatState.HittableEnemies
            .Where(e => e != null && e.IsHittable)
            .ToList();
        if (candidates.Count == 0) return;

        target = candidates[Random.Shared.Next(candidates.Count)];
    }
}

/// <summary>
///     太极重定向补丁 (多目标): 覆盖 <see cref="CreatureCmd.Damage" /> 的 4 个
///     IEnumerable&lt;Creature&gt; 重载 —— 敌方 AI 的攻击动作把目标当作列表传入
///     (即使单体也是单元素列表), 这才是 2026-09-20 之前太极从未生效的根因。
///     改写规则与单体版相同: 列表中带 <see cref="TaiChiMark" /> 的玩家元素,
///     替换为一名随机敌人; 其余元素原样保留。
/// </summary>
[HarmonyPatch]
internal static class TaiChiRedirectMultiPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var m in AccessTools.GetDeclaredMethods(typeof(CreatureCmd)))
        {
            if (m.Name != nameof(CreatureCmd.Damage)) continue;
            var ps = m.GetParameters();
            if (ps.Length >= 2 && ps[1].ParameterType == typeof(IEnumerable<Creature>))
                yield return m;
        }
    }

    // 用索引绑定 __1 (第二参 targets), 不依赖参数名。
    private static void Prefix(ref IEnumerable<Creature> __1)
    {
        if (__1 == null) return;
        var list = __1 as IList<Creature> ?? __1.ToList();
        var changed = false;
        for (var i = 0; i < list.Count; i++)
        {
            var t = list[i];
            if (t == null || !t.IsPlayer) continue;
            var mark = t.GetPower<TaiChiMark>();
            if (mark == null || mark.Amount <= 0) continue;
            var combatState = t.CombatState;
            if (combatState == null) continue;
            var candidates = combatState.HittableEnemies
                .Where(e => e != null && e.IsHittable)
                .ToList();
            if (candidates.Count == 0) continue;
            list[i] = candidates[Random.Shared.Next(candidates.Count)];
            changed = true;
        }
        if (changed)
            __1 = list;
    }
}
