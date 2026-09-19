using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     拔罐: 每回合开始时, 随机移除自身 Amount 层负面状态 (1 → 2 层)。
///     <para>
///         与「涤荡」(一次性全清) 和「舒筋」(转移给敌人) 分工: 拔罐是慢工 ——
///         回合数越多清得越多, 适合长期挂负面 (中毒/虚弱) 的战斗。
///     </para>
/// </summary>
public sealed class BaGuan : yylPowerModel
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
        var toRemove = (int)Amount;
        if (toRemove <= 0) return;

        for (var i = 0; i < toRemove; i++)
        {
            var debuffs = Owner.Powers
                .Where(p => p.Type == PowerType.Debuff && p.Amount > 0)
                .ToList();
            if (debuffs.Count == 0) return;

            var pick = debuffs[Random.Shared.Next(debuffs.Count)];
            await PowerCmd.ModifyAmount(ctx, pick, -1m, Owner, null);
        }
    }
}
