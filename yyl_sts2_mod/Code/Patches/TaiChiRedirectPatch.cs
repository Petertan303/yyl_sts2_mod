using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

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
            RedirectAllOrNothing(ref __1, ref dealer, props);
        }
    }

    /// <summary>(ctx, targets, damageVar, dealer [, cardSource, cardPlay])。</summary>
    [HarmonyPatch]
    internal static class DamageVarDealer
    {
        private static IEnumerable<MethodBase> TargetMethods() => MultiTargets(requireProps: false);

        private static void Prefix(ref IEnumerable<Creature> __1, ref Creature dealer)
        {
            RedirectAllOrNothing(ref __1, ref dealer, null);
        }
    }

    /// <summary>
    ///     ★★ 多目标重定向 (2026-10-19 晚终版: 守护全队)。
    ///     <para>
    ///         演进史: ① 9-22 之前 = 部分转嫁 + dealer 置 null (队友来源判定失效);
    ///         ② 9-22 = "全有或全无" —— 联机 AoE 必然失败, 是"联机太极不生效"的根因;
    ///         ③ 当天上午 = 个人防护 (只转目标是自己带的印记);
    ///         ④ 晚终版 (用户定调): **守护所有队友** —— 任一玩家带印记,
    ///         敌人真实攻击的所有玩家目标 (含没印记的队友) 全部转嫁。
    ///     </para>
    ///     <para>
    ///         配套调整: 太极改为 3 费 3 炁 (守护全队强度大增, 用户定调);
    ///         B 路径的转移次数 = 带印记玩家数 (N 次独立转嫁, 见 TaiChiMark.FlushPending)。
    ///         dealer 整次置 null 的铁律不变; 队友不掉血, 来源判定失效无所谓。
    ///     </para>
    /// </summary>
    private static void RedirectAllOrNothing(ref IEnumerable<Creature> targets, ref Creature dealer, ValueProp? props)
    {
        if (targets == null) return;
        var list = targets as IList<Creature> ?? targets.ToList();
        if (list.Count == 0) return;

        // 逐个目标选替身; 任一玩家目标能转 (任一玩家带印记即满足), 则所有玩家目标都转。
        var victims = new Creature?[list.Count];
        var any = false;
        for (var i = 0; i < list.Count; i++)
        {
            var victim = TaiChiRedirectUtil.PickVictim(list[i], dealer, props, true);
            victims[i] = victim;
            if (victim != null) any = true;
        }

        if (!any) return;

        // 第一遍没选出来的 (无印记队友): 此时任一玩家带印记 ⇒ 团队语义下必然能选出来。
        for (var i = 0; i < list.Count; i++)
        {
            if (victims[i] != null) continue;
            victims[i] = TaiChiRedirectUtil.PickVictim(list[i], dealer, props, true);
            if (victims[i] == null) victims[i] = list[i]; // 理论不可达; 保险: 维持原目标
        }

        for (var i = 0; i < list.Count; i++) list[i] = victims[i]!;
        targets = list;
        dealer = null;
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

/// <summary>
///     ★ v0.113.1 新增路径 (2026-11-15): 引擎把 <c>AttackCommand.Execute</c> 的伤害结算改到
///     <c>CreatureCmd.Damage(ctx, IEnumerable&lt;DamageTargetData&gt;, amount, props, dealer,
///     cardSource, cardPlay, modifyDamageHookType)</c>。上面所有补丁覆盖的是
///     <c>IEnumerable&lt;Creature&gt;</c> / 单个 <c>Creature</c> 重载, <b>碰不到这条新重载</b> ——
///     实测表现就是「印记挂上了 (日志有 [太极] 印记已施加), 敌方攻击却照常打在玩家身上」
///     (日志里没有 [太极] 重定向)。反汇编确认引擎里唯一直接调用该重载的外部调用点就是
///     <c>Commands.Builders.AttackCommand+&lt;Execute&gt;d__91::MoveNext</c>,
///     其余 Damage 重载也会在内部汇入它。
///     <para>
///         <c>DamageTargetData</c> 是 public record struct, <c>target</c> 是 public 字段,
///         可以直接改写; 其余字段 (hitVfx / playVfxOnCreatureCenter) 保持不变。
///         转嫁判据与 "requireDealer" 语义沿用 <see cref="TaiChiRedirectUtil" />,
///         并保留「任一玩家带印记 ⇒ 所有玩家目标都转」的守护全队语义、dealer 置 null 铁律。
///     </para>
/// </summary>
internal static class TaiChiRedirectTargetDataPatch
{
    [HarmonyPatch]
    internal static class TargetDataAmountDealer
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var m in AccessTools.GetDeclaredMethods(typeof(CreatureCmd)))
            {
                if (m.Name != nameof(CreatureCmd.Damage)) continue;
                var ps = m.GetParameters();
                // (ctx, targetData, amount, props, dealer, cardSource, cardPlay, hookType)
                if (ps.Length < 5) continue;
                if (ps[1].ParameterType != typeof(IEnumerable<DamageTargetData>)) continue;
                if (ps[4].ParameterType != typeof(Creature)) continue; // dealer
                yield return m;
            }
        }

        // 全部按参数索引绑定 (__1 = targetData, __3 = props, __4 = dealer), 不依赖参数名。
        private static void Prefix(ref IEnumerable<DamageTargetData> __1, ValueProp __3, ref Creature __4)
        {
            RedirectTargetData(ref __1, __3, ref __4);
        }
    }

    private static void RedirectTargetData(
        ref IEnumerable<DamageTargetData> targetData, ValueProp props, ref Creature dealer)
    {
        if (targetData == null) return;
        var list = targetData as IList<DamageTargetData> ?? targetData.ToList();
        if (list.Count == 0) return;

        var victims = new Creature?[list.Count];
        var any = false;
        for (var i = 0; i < list.Count; i++)
        {
            victims[i] = TaiChiRedirectUtil.PickVictim(list[i].target, dealer, props, true);
            if (victims[i] != null) any = true;
        }
        if (!any) return;

        // 守护全队: 第一遍没选出来的 (无印记队友) 在团队语义下也一并转嫁。
        for (var i = 0; i < list.Count; i++)
        {
            if (victims[i] != null) continue;
            victims[i] = TaiChiRedirectUtil.PickVictim(list[i].target, dealer, props, true)
                         ?? list[i].target; // 理论不可达; 保险: 维持原目标
        }

        for (var i = 0; i < list.Count; i++)
        {
            var data = list[i];
            data.target = victims[i]!;
            list[i] = data;
        }
        targetData = list;
        dealer = null; // ★ 无来源: 蜂巢等"按攻击者反制"的怪物 Power 直接跳过。
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
        var combatState = target.CombatState;
        if (combatState == null) return null;

        // ★[2026-10-19 晚 守护全队语义, 用户定调] 只要**任一玩家**带太极印记, 敌方的真实攻击
        //   就可以转嫁 —— 包括打在没印记的队友身上。配合 TaiChi 的 3 费 3 炁定价。
        var marked = target.GetPower<TaiChiMark>();
        if (marked is not { Amount: > 0 } && !AnyPlayerMarked(combatState)) return null;

        // 只转嫁"真实攻击"伤害 (倒映/蜂巢同款判定): 反噬/Power 伤害不是 Move。
        if (props.HasValue && !props.Value.IsPoweredAttack()) return null;
        // 只转嫁敌方来源 (无来源 = 中毒/状态牌/环境类, 不转嫁)。
        // dealer 有值按阵营判; dealer 为空时用"当前是否敌方回合"兜底
        // (联机下部分伤害命令拿不到 dealer; 敌方回合里玩家挨的打必然来自敌人)。
        if (dealer != null && dealer.Side == target.Side) return null;
        if (dealer == null && requireDealer && !InEnemyTurn(target)) return null;

        var candidates = combatState.HittableEnemies
            .Where(e => e != null && e.IsHittable)
            .ToList();
        if (candidates.Count == 0) return null;
        // ★必须用引擎的确定性 RNG, 不能用 Random.Shared (2026-10-15 联机 desync 修复):
        //   联机是"各端独立模拟 + 定时同步 RNG 状态"的 lockstep 架构
        //   (见 CombatStateSynchronizer.OnSyncRngMessageReceived), 每个客户端都会跑
        //   到这个前缀。Random.Shared 是进程级非确定随机 → 各端选出不同替身 →
        //   伤害去向不同 → 数据不同步。原版"随机目标"卡走的是声明式
        //   TargetType.RandomEnemy / AttackCommand.TargetingRandomOpponents,
        //   随机发生在引擎的同步流程内, 所以不会 desync。
        var victim = yylRng.PickCombatTarget(combatState, candidates);
        MainFile.Logger.Info(
            $"[太极] 重定向: 目标={target.Name} 来源={(dealer == null ? "null" : dealer.Name)} → 替身={victim?.Name} 候选={candidates.Count}");
        return victim;
    }

    /// <summary>任一存活玩家带有效太极印记 (守护全队语义的门槛)。</summary>
    private static bool AnyPlayerMarked(ICombatState combatState)
    {
        foreach (var p in combatState.Players)
        {
            if (p?.Creature == null || !p.Creature.IsAlive) continue;
            if (p.Creature.GetPower<TaiChiMark>() is { Amount: > 0 }) return true;
        }
        return false;
    }

    /// <summary>dealer 缺失时的兜底判据: 当前处于敌方回合 ⇒ 这次伤害只能来自敌人。</summary>
    private static bool InEnemyTurn(Creature target)
    {
        try
        {
            return target.CombatState?.CurrentSide == CombatSide.Enemy;
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[太极] 读取当前回合失败, 按\"非敌方回合\"处理: {e.Message}");
            return false;
        }
    }
}
