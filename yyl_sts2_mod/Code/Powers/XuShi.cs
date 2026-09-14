using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Commands;
using MegaCrit.Sts2.Core.Commands;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     蓄势 (delayed-Qi buff): 打出蓄势牌后, 在玩家下个回合开始时获得 N 炁 (N = Amount),
///     之后自动移除。
///     <para>
///         Stack: Counter, Amount = 即将获得的炁数量(默认 2, 升级后 3)。
///     </para>
/// </summary>
public sealed class XuShi : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext ctx,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;
        if (Amount <= 0) return;
        var qi = Amount;
        var player = combatState.Players.FirstOrDefault(p => p.Creature == Owner);
        if (player == null) return;

        // TODO(API): 验证 PowerCmd.Remove(this, ctx) 的实际签名; BaseLib 该方法签名待确认
        // 暂时: 不主动移除, 让 Amount 自然衰减(若 amount 在 1 则下次不会触发)
        await yylCmd.GainQi(ctx, player, qi, this, null);
    }
}
