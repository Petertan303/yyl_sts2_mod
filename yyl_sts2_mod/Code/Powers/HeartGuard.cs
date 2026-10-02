using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>心防：本回合来自奶龙的伤害减半。</summary>
public sealed class HeartGuard : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target == Owner && !props.HasFlag(ValueProp.Unpowered) && yylNailong.IsNailongSource(dealer))
            return 0.5m;
        return 1m;
    }
    //
    // public override async Task AfterSideTurnEnd(
    //     PlayerChoiceContext ctx,
    //     CombatSide side,
    //     IEnumerable<Creature> participants)
    // {
    //     if (side == Owner.Side)
    //         await PowerCmd.Remove(this);
    // }

    /// <summary>
    ///     单回合过期 —— 必须用<b>玩家级</b>钩子而非 side 级钩子。
    ///     联机时所有玩家同属一个 <c>CombatSide</c>, <c>side == Owner.Side</c> 区分不出是谁的回合,
    ///     导致任意一人触发回合开始就把<b>全队</b>心防抹掉; 老农功/炁婴插入额外回合时会提前一轮蒸发。
    ///     改用 <c>player == Owner.Player</c> 后, 只在本人回合开始时结算, 联机互不干扰。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
            await PowerCmd.Remove(this);
        await base.AfterPlayerTurnStart(choiceContext, player);
    }
}
