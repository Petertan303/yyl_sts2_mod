using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     蓄势 (delayed-Qi buff): 打出蓄势牌后, 在玩家下个回合开始时获得 N 炁 (N = Amount),
///     之后自动移除。
///     <para>
///         Stack: Counter, Amount = 即将获得的炁数量(默认 2, 升级后 3)。
///     </para>
///     <para>
///         ★2026-11-18 时机修正: 原挂 <c>BeforeSideTurnStart</c>(清格挡前), 得炁经温养换算出的
///         格挡紧接着被引擎 <c>ClearBlock</c> 抹掉 (观感: 炁到账、格挡冒一下就没了)。
///         挪到 <c>AfterPlayerTurnStart</c>(清格挡之后), 与 QiHai / 金光衰减同款;
///         守卫用 <c>player == Owner.Player</c> 而非 side (联机时 side 判不出是谁的回合)。
///     </para>
/// </summary>
public sealed class StoredMight : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext ctx, Player player)
    {
        if (Owner == null || player == null || player != Owner.Player) return;
        if (Amount <= 0) return;

        await yylCmd.GainQi(ctx, player, Amount, this, null);
        await PowerCmd.Remove(this);
    }
}
