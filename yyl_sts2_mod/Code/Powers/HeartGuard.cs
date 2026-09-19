using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
    public override PowerStackType StackType => PowerStackType.Single;

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

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == Owner.Side)
            PowerCmd.Remove(this);
        return base.BeforeSideTurnStart(choiceContext, side, participants, combatState);
    }
}
