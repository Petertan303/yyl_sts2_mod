using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace yyl_sts2_mod.Code.Commands;

/// <summary>
///     记录每名玩家的**产炁记录** (战斗中 / 本回合的累计数量 + 最近一次产炁的回合)。
///     在 <see cref="yylCmd.GainQi" /> 内打标 —— 它是全 mod 唯一的产炁入口
///     (卡牌 / 遗物 / Power 的所有产炁都经它), 因此无需额外 Harmony 补丁。
///     <para>
///         2026-10-03 扩展: 原版只记"本回合是否产过炁"(布尔, 供崩拳用),
///         现增加**累计数量**统计, 供"按本回合/本场战斗获炁量成长"的卡使用:
///         思维膨大(本回合获炁数 → 段数)、生生不息(本场获炁总数 → 加伤)。
///     </para>
/// </summary>
public static class QiGainTracker
{
    /// <summary>单个玩家的产炁记录。Combat 为 null 表示无记录(未战斗/已换战斗)。</summary>
    private sealed class GainRecord
    {
        public ICombatState? Combat;
        public int Round;

        /// <summary>本回合累计获得的炁量。</summary>
        public int RoundAmount;

        /// <summary>本场战斗累计获得的炁量 (跨回合累加, 战斗结束随 Combat 失效)。</summary>
        public int CombatAmount;
    }

    private static readonly Dictionary<Player, GainRecord> Records = new();

    /// <summary>打标: 记录本次产炁(累加本场计数; 本回合计数按回合重置)。amount 为本次获得的炁量。</summary>
    public static void Mark(Player player, ICombatState? combatState, int amount = 0)
    {
        if (player == null || combatState == null) return;

        if (!Records.TryGetValue(player, out var rec) || !ReferenceEquals(rec.Combat, combatState))
        {
            // 新战斗 ⇒ 重置全部计数。
            rec = new GainRecord { Combat = combatState };
            Records[player] = rec;
        }

        // ★ 2026-10-07 修「思维膨大段数=历史累计」: RoundAmount 之前只累加、**从不在回合切换时归零**,
        //   于是第 2 回合产炁后读到的仍是「第 1 回合 + 第 2 回合」的总量
        //   (例: 第1回合拿3 点 → RoundAmount=3; 第2回合拿 2 点 → RoundAmount=5,
        //    思维膨大按 5 段结算)。而 RoundAmount 语义是"**本回合**获炁量"。
        //   修法: 回合号变化时先把上一回合的残留清零, 再累加本次。
        //   CombatAmount 是"本场战斗"总量, **故意不清**。
        if (rec.Round != combatState.RoundNumber)
        {
            rec.Round = combatState.RoundNumber;
            rec.RoundAmount = 0;
        }

        rec.RoundAmount += amount;
        rec.CombatAmount += amount;
    }

    /// <summary>该玩家在本场战斗的当前回合内是否获得过炁。</summary>
    /// <para>
    ///     2026-10-07 修正: <c>Mark</c> 现在会在回合切换时清零 <c>RoundAmount</c>,
    ///     因此本方法必须<b>同时校验 rec.Round</b>—— 否则"上一回合产过炁、本回合还没产"
    ///     期间 <c>RoundAmount</c> 仍保留旧值, 会让崩拳这类"本回合产过炁才减费"的牌
    ///     在错误的时机触发。
    /// </para>
    /// </summary>
    public static bool GainedThisRound(Player? player, ICombatState? combatState)
    {
        if (player == null || combatState == null) return false;
        return Records.TryGetValue(player, out var rec)
               && ReferenceEquals(rec.Combat, combatState)
               && rec.Round == combatState.RoundNumber
               && rec.RoundAmount > 0;
    }

    /// <summary>该玩家<strong>本回合</strong>累计获得的炁量 (用于"按本回合产炁成长")。</summary>
    public static int RoundAmount(Player? player, ICombatState? combatState)
    {
        if (player == null || combatState == null) return 0;
        if (!Records.TryGetValue(player, out var rec)) return 0;
        if (!ReferenceEquals(rec.Combat, combatState) || rec.Round != combatState.RoundNumber) return 0;
        return rec.RoundAmount;
    }

    /// <summary>该玩家<strong>本场战斗</strong>累计获得的炁量 (用于"按战斗内成长")。</summary>
    public static int CombatAmount(Player? player, ICombatState? combatState)
    {
        if (player == null || combatState == null) return 0;
        if (!Records.TryGetValue(player, out var rec)) return 0;
        return ReferenceEquals(rec.Combat, combatState) ? rec.CombatAmount : 0;
    }
}