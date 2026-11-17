using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace yyl_sts2_mod.Code.Commands;

/// <summary>
///     记录每名玩家<strong>本场战斗累计获得的金光</strong>（只增不减，与金光当前存量无关）。
///     <para>
///         用于「寸劲」—— 按历史获炁量成长的攻击牌。设计意图（2026-10-07 用户定调）：
///         寸劲要当<B>廉价的上限</B>（对boss 有威慑），又要在<b>前期道中不卡手</b>。
///         读「存量炁」会被耗炁牌（燃炁/天火/散炁）反复清零，导致同一种牌在不同回合威力天差地别；
///         改成读「历史累计金光」后，威力只随本场战斗推进单调上涨，<b>稳定可预期</b>——
///         这与QiGainTracker.CombatAmount（寸劲的原型机制）是同一套范式，只是换成金光。
///     </para>
///     <para>
///         ★ 为什么必须靠 Harmony 拦 <c>PowerCmd.Apply</c>，而不能挂在 Power 自己的生命周期钩子上：
///         <c>PowerModel.BeforeApplied</c> / <c>AfterApplied</c> 的引擎文档都明写
///         <b>"Does not run if the owner already has this power, and its amount was just changed"</b>
///         —— 即<b>只在首次施加时触发，叠层（ModifyAmount 分支）根本不进这两个钩子</b>。
///         而金光在全库有 7+ 个施加入口（金光符/引光/天祭/炼化/金光壁/金龟…），
///         逐个改既漏又脆。改用非泛型公共出口
///         <c>PowerCmd.Apply(ctx, power, target, amount, applier, cardSource, silent)</c>——
///         所有泛型重载与 <c>ModifyAmount</c> 叠层最终都汇到它（见反编译实证），
///         一次拦截即可覆盖全部路径，且叠层也能统计到。
///     </para>
/// </summary>
public static class AegisGainTracker
{
    /// <summary>单个玩家的金光获得记录。Combat 为 null 表示无记录（未战斗/已换战斗）。</summary>
    private sealed class AegisRecord
    {
        public ICombatState? Combat;

        /// <summary>本场战斗累计获得的金光量（跨回合累加，战斗结束随 Combat 失效）。</summary>
        public int CombatAmount;
    }

    private static readonly Dictionary<Player, AegisRecord> Records = new();

    /// <summary>
    ///     打标：记录本次获得的金光。<paramref name="amount" /> 为本次实际入账量
    ///     （<b>已经过滤掉负数</b>—— 消耗金光走的是同一个Apply 出口，不扣累计值）。
    /// </summary>
    public static void Mark(Player? player, ICombatState? combatState, int amount)
    {
        if (player == null || combatState == null || amount <= 0) return;

        if (!Records.TryGetValue(player, out var rec) || !ReferenceEquals(rec.Combat, combatState))
        {
            // 新战斗 ⇒ 重置（与 QiGainTracker 同一套失效策略，靠 Combat 引用比对而非手动清）。
            rec = new AegisRecord { Combat = combatState };
            Records[player] = rec;
        }

        rec.CombatAmount += amount;
    }

    /// <summary>该玩家<strong>本场战斗</strong>累计获得的金光量（用于「按战斗内成长」）。</summary>
    public static int CombatAmount(Player? player, ICombatState? combatState)
    {
        if (player == null || combatState == null) return 0;
        if (!Records.TryGetValue(player, out var rec)) return 0;
        return ReferenceEquals(rec.Combat, combatState) ? rec.CombatAmount : 0;
    }
}