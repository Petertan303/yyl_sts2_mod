using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     炁海: 每回合开始时获得 Amount 点炁 (1 → 2 点)。产炁流的稳定来源。
///     <para>
///         与「金光护体源」(每回合 +1 层金光) 对称: 一个是攻的长期投资, 一个是防的长期投资。
///         炁是对数增伤, 所以这里的"每回合 +1"更像是给天火/丹噬/耗炁牌供血的引擎, 而非直接伤害。
///     </para>
/// </summary>
public sealed class QiHai : yylPowerModel
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
        var amount = (int)Amount;
        if (amount <= 0) return;
        if (Owner.Player == null) return;
        await yylCmd.GainQi(ctx, Owner.Player, amount, this, null);
    }
}
