using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;

namespace yyl_sts2_mod.Code.Powers;

public sealed class InverseLife : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    private static readonly SpireField<Creature, bool> _firstBlockUsed = new(() => false);
    
    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext ctx,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side)
            return;
        _firstBlockUsed[Owner] = false;
        if (Amount >= 2)
            await CreatureCmd.Heal(Owner, 6);
        if (Amount >= 3)
            await PowerCmd.Apply<IntangiblePower>(ctx, Owner, 1, Owner, null);
    }

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer == Owner && !props.HasFlag(ValueProp.Unpowered))
            return 1m + 0.4m * Amount;
        return 1m;
    }

    public decimal ModifyBlockMultiplicativeCompability(
        Creature target,
        decimal blockAmount,
        ValueProp props,
        Creature? source,
        CardModel? cardSource)
    {
        if (target == Owner && Amount >= 1 && !_firstBlockUsed[Owner])
        {
            _firstBlockUsed[Owner] = true;
            return 2m;
        }
        return 1m;
    }
}