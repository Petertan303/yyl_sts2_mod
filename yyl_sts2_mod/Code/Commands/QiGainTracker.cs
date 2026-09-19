using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;

namespace yyl_sts2_mod.Code.Commands;

/// <summary>
///     记录每名玩家"最近一次获得炁"的 (战斗, 回合)。
///     在 <see cref="yylCmd.GainQi" /> 内打标 —— 它是全 mod 唯一的产炁入口
///     (卡牌 / 遗物 / Power 的所有产炁都经它), 因此无需额外 Harmony 补丁。
///     供"若你本回合获得过炁"类条件牌查询 (如崩拳)。
/// </summary>
public static class QiGainTracker
{
    private static readonly Dictionary<Player, (ICombatState Combat, int Round)> LastGain = new();

    public static void Mark(Player player, ICombatState? combatState)
    {
        if (player == null || combatState == null) return;
        LastGain[player] = (combatState, combatState.RoundNumber);
    }

    /// <summary>该玩家在本场战斗的本回合内是否获得过炁。</summary>
    public static bool GainedThisRound(Player? player, ICombatState? combatState)
    {
        if (player == null || combatState == null) return false;
        return LastGain.TryGetValue(player, out var entry)
               && ReferenceEquals(entry.Combat, combatState)
               && entry.Round == combatState.RoundNumber;
    }
}
