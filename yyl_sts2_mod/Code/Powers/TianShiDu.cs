using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     天师度 (debuff side of 天师度 card): 每次我方回合开始时失去 2 点炁 / 层。
///     类似于 Wraith Form: 卡牌给予正向 buff(获得 10 炁 + 3 金光咒)与本 debuff;
///     本 Power 仅承载 debuff 副作用,正向 buff 在卡牌的 OnPlay 中直接生效。
///     <para>
///         设计为"逐渐变重"的负面效果,目前实现为每回合固定 -2 炁 / 层。后续可改为
///         基于已存在回合数累加(例如 -1, -2, -3 ...)。
///     </para>
///     <remarks>
///         TODO(API): 临时用 <c>BeforeSideTurnStart</c> 监听"我方回合开始"作为
///         "上回合结束"的代理。BaseLib 真正的"回合结束"钩子(<c>AfterSideTurnEnd</c> /
///         <c>OnTurnEnd</c>)待用户确认后改回。
///     </remarks>
/// </summary>
public sealed class TianShiDu : yylPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Base Qi lost per stack of TianShiDu at the end of the player's turn.</summary>
    public const int BaseQiLoss = 2;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext ctx,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;
        var player = combatState.Players.FirstOrDefault(p => p.Creature == Owner);
        if (player == null) return;

        var loss = BaseQiLoss * Amount;
        if (loss <= 0) return;
        await yylCmd.LoseQi(ctx, player, loss, this, null);
    }
}
