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
    // 逆生三重的"隔回合"计数 (每当我方回合开始 +1, 奇数回合发无实体)。
    private static readonly SpireField<Creature, int> _thirdTurnCount = new(() => 0);

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext ctx,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side)
            return;
        _firstBlockUsed[Owner] = false;

        if (Amount >= 3)
        {
            // ★逆生三重 (2026-09-21 用户定调): 每隔一回合获得 1 层无实体,
            //   并且**不再享有逆生二重的回血** —— 三重以"无实体"取代"回血"作为收益。
            //   计数从进入三重后的第一个回合起算 (奇数回合给, 即立刻吃到第一层),
            //   想改成"第二回合才给"把下面的 %2 判断取反即可。
            var n = _thirdTurnCount[Owner] + 1;
            _thirdTurnCount[Owner] = n;
            if (n % 2 == 1)
                await PowerCmd.Apply<IntangiblePower>(ctx, Owner, 1, Owner, null);
            return;
        }

        if (Amount >= 2)
            await CreatureCmd.Heal(Owner, 6);
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