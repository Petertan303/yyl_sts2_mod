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
///     太极重定向补丁: 给 <see cref="CreatureCmd.Damage" /> 的全部"单体目标"重载
///     加 Harmony 前缀 —— 当目标是你、且你身上有 <see cref="TaiChiMark" /> 时,
///     把目标改写为一名随机敌人 (你不受伤害, 敌人替你挨打)。
///     <para>
///         说明:
///         - 只改 target 引用, 伤害数值/来源/动画管线全部原样, 因此格挡、易伤、
///           无实体等防御结算对"替身"自然生效。
///         - 幂等: 重定向后的 target 已不是玩家, 其他重载的前缀不会二次触发。
///         - 覆盖 6 个单体重载 (decimal 与 DamageVar 两种入参); IEnumerable 多目标
///           重载不处理 (多人为队友挡伤害的语义本就不同)。
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
