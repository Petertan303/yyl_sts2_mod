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
///     太极重定向补丁 (2026-09-20 重写版): 给 <see cref="CreatureCmd.Damage" /> 的
///     全部重载加 Harmony 前缀 —— 当目标是带 <see cref="TaiChiMark" /> 的你、且这次
///     伤害是<b>敌方真实攻击</b>时, 把目标改写为一名随机敌人 (你不受伤害, 敌人替你
///     原封不动地挨下这一击)。
///     <para>
///         ★★两条铁律 (都来自实战卡死, 2026-09-20):
///         ① <b>重定向的同时必须把 dealer 置 null</b> —— 若保留原攻击者, 就变成
///         "怪物攻击怪物", 蜂群术士的蜂巢 (PersonalHivePower, 受攻击时给攻击者塞
///         晕眩牌) 会往怪物手里塞牌直接卡死。反汇编确认蜂巢对 dealer==null 的伤害
///         第二道门直接 return, 无来源重定向完全安全。
///         ② 只转嫁 <see cref="ValuePropExtensions.IsPoweredAttack" /> 且 dealer 为
///         敌方来源的伤害 —— 中毒/状态/自伤类一律不动。
///     </para>
///     <para>
///         结构说明: Harmony 前缀只允许绑定目标方法真实存在的参数, 因此按
///         "有/无 props""有/无 dealer"拆成 4 个单体补丁类 + 2 个多目标补丁类。
///         敌方 AI 的攻击把目标当列表传入 (即使单体也是单元素列表), 主路径是
///         多目标补丁; <see cref="TaiChiMark.AfterDamageReceived" /> 事件转移
///         (倒映式) 保留作未覆盖路径的兜底, 两条路径天然互斥
///         (重定向成功则玩家掉血为 0, 事件钩子因 target != Owner 跳过)。
///     </para>
/// </summary>
internal static class TaiChiRedirectPatch
{
    /// <summary>(ctx, target, amount, props, dealer [, cardSource, cardPlay]) 两个重载。</summary>
    private static readonly Type[][] AmountDealerSigs =
    {
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature) },
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay) },
    };

    /// <summary>(ctx, target, damageVar, dealer [, cardSource, cardPlay]) 两个重载。</summary>
    private static readonly Type[][] DamageVarDealerSigs =
    {
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(DamageVar), typeof(Creature) },
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(DamageVar), typeof(Creature), typeof(CardModel), typeof(CardPlay) },
    };

    /// <summary>(ctx, target, amount, props, cardSource, cardPlay) —— 无 dealer。</summary>
    private static readonly Type[] AmountSourcelessSig =
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel), typeof(CardPlay) };

    /// <summary>(ctx, target, damageVar, cardSource, cardPlay) —— 无 dealer 无 props。</summary>
    private static readonly Type[] DamageVarSourcelessSig =
        new[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(DamageVar), typeof(CardModel), typeof(CardPlay) };

    private static IEnumerable<MethodBase> SigMethods(IEnumerable<Type[]> sigs)
    {
        foreach (var sig in sigs)
        {
            var method = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), sig);
            if (method != null)
                yield return method;
        }
    }

    /// <summary>单体 + amount + props + dealer: 主力补丁之一。</summary>
    [HarmonyPatch]
    internal static class AmountDealer
    {
        private static IEnumerable<MethodBase> TargetMethods() => SigMethods(AmountDealerSigs);

        private static void Prefix(ref Creature target, ref Creature dealer, ValueProp props)
        {
            var victim = TaiChiRedirectUtil.PickVictim(target, dealer, props, true);
            if (victim == null) return;
            target = victim;
            dealer = null; // ★ 无来源: 蜂巢等"按攻击者反制"的怪物 Power 直接跳过。
        }
    }

    /// <summary>单体 + damageVar + dealer。</summary>
    [HarmonyPatch]
    internal static class DamageVarDealer
    {
        private static IEnumerable<MethodBase> TargetMethods() => SigMethods(DamageVarDealerSigs);

        private static void Prefix(ref Creature target, ref Creature dealer)
        {
            var victim = TaiChiRedirectUtil.PickVictim(target, dealer, null, true);
            if (victim == null) return;
            target = victim;
            dealer = null;
        }
    }

    /// <summary>单体 + amount + props, 无 dealer (本就无来源, 无需置空)。</summary>
    [HarmonyPatch]
    internal static class AmountSourceless
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var method = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), AmountSourcelessSig);
            if (method != null)
                yield return method;
        }

        private static void Prefix(ref Creature target, ValueProp props)
        {
            var victim = TaiChiRedirectUtil.PickVictim(target, null, props, false);
            if (victim == null) return;
            target = victim;
        }
    }

    /// <summary>单体 + damageVar, 无 dealer 无 props (旧版行为: 无信息可判, 直接转)。</summary>
    [HarmonyPatch]
    internal static class DamageVarSourceless
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var method = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), DamageVarSourcelessSig);
            if (method != null)
                yield return method;
        }

        private static void Prefix(ref Creature target)
        {
            var victim = TaiChiRedirectUtil.PickVictim(target, null, null, false);
            if (victim == null) return;
            target = victim;
        }
    }
}

/// <summary>
///     太极重定向补丁 (多目标): 覆盖 <see cref="CreatureCmd.Damage" /> 的 4 个
///     IEnumerable&lt;Creature&gt; 重载 —— 敌方 AI 的攻击动作把目标当作列表传入
///     (即使单体也是单元素列表), 这是太极对敌方攻击生效的主路径。
///     列表中带 <see cref="TaiChiMark" /> 的玩家元素替换为随机敌人; 只要有任何
///     元素被重定向, 整次伤害的 dealer 置 null (见 <see cref="TaiChiRedirectPatch" />
///     顶部铁律①)。Harmony 前缀只允许绑定目标方法真实存在的参数, 按
///     "有/无 props"拆成两个类。
/// </summary>
internal static class TaiChiRedirectMultiPatch
{
    /// <summary>(ctx, targets, amount, props, dealer [, cardSource, cardPlay]): 敌方 AI 主路径。</summary>
    [HarmonyPatch]
    internal static class AmountDealer
    {
        private static IEnumerable<MethodBase> TargetMethods() => MultiTargets(requireProps: true);

        // 用索引绑定 __1 (第二参 targets), 不依赖参数名; dealer/props 按名绑定。
        private static void Prefix(ref IEnumerable<Creature> __1, ref Creature dealer, ValueProp props)
        {
            if (__1 == null) return;
            var list = __1 as IList<Creature> ?? __1.ToList();
            var changed = false;
            for (var i = 0; i < list.Count; i++)
            {
                var victim = TaiChiRedirectUtil.PickVictim(list[i], dealer, props, true);
                if (victim == null) continue;
                list[i] = victim;
                changed = true;
            }
            if (!changed) return;
            __1 = list;
            dealer = null;
        }
    }

    /// <summary>(ctx, targets, damageVar, dealer [, cardSource, cardPlay])。</summary>
    [HarmonyPatch]
    internal static class DamageVarDealer
    {
        private static IEnumerable<MethodBase> TargetMethods() => MultiTargets(requireProps: false);

        private static void Prefix(ref IEnumerable<Creature> __1, ref Creature dealer)
        {
            if (__1 == null) return;
            var list = __1 as IList<Creature> ?? __1.ToList();
            var changed = false;
            for (var i = 0; i < list.Count; i++)
            {
                var victim = TaiChiRedirectUtil.PickVictim(list[i], dealer, null, true);
                if (victim == null) continue;
                list[i] = victim;
                changed = true;
            }
            if (!changed) return;
            __1 = list;
            dealer = null;
        }
    }

    private static IEnumerable<MethodBase> MultiTargets(bool requireProps)
    {
        foreach (var m in AccessTools.GetDeclaredMethods(typeof(CreatureCmd)))
        {
            if (m.Name != nameof(CreatureCmd.Damage)) continue;
            var ps = m.GetParameters();
            if (ps.Length < 2 || ps[1].ParameterType != typeof(IEnumerable<Creature>)) continue;
            var hasProps = ps.Any(p => p.ParameterType == typeof(ValueProp));
            if (hasProps != requireProps) continue;
            // 只挂带 dealer 的多目标重载。
            if (!ps.Any(p => p.ParameterType == typeof(Creature) && p.Name == "dealer")) continue;
            yield return m;
        }
    }
}

/// <summary>太极重定向共享判定: 该次伤害是否应转嫁、替身是谁。</summary>
internal static class TaiChiRedirectUtil
{
    /// <param name="target">本次伤害的当前目标。</param>
    /// <param name="dealer">伤害来源 (可空)。</param>
    /// <param name="props">伤害 ValueProp (重载没有该参数时传 null)。</param>
    /// <param name="requireDealer">有 dealer 参数的重载必须为真: 无来源/友方来源不转嫁。</param>
    internal static Creature? PickVictim(
        Creature? target,
        Creature? dealer,
        ValueProp? props,
        bool requireDealer)
    {
        if (target == null || !target.IsPlayer) return null;
        var mark = target.GetPower<TaiChiMark>();
        if (mark == null || mark.Amount <= 0) return null;
        // 只转嫁"真实攻击"伤害 (倒映/蜂巢同款判定): 反噬/Power 伤害不是 Move。
        if (props.HasValue && !props.Value.IsPoweredAttack()) return null;
        // 只转嫁敌方来源 (无来源 = 中毒/状态牌/环境类, 不转嫁)。
        if (requireDealer && (dealer == null || dealer.Side == target.Side)) return null;

        var combatState = target.CombatState;
        if (combatState == null) return null;
        var candidates = combatState.HittableEnemies
            .Where(e => e != null && e.IsHittable)
            .ToList();
        if (candidates.Count == 0) return null;
        return candidates[Random.Shared.Next(candidates.Count)];
    }
}
