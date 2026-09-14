using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     老农功: 获得炁时, 额外获得 1 点炁(即 "获得 X 炁" 实际变为 X+1)。
///     Hook-based: implements <see cref="IGainQi" /> so the bonus stacks with the
///     normal Qi gain chain and any other future Qi-gain modifiers.
/// </summary>
public sealed class RusticRoot : yylPowerModel, IGainQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int ModifyQiGain(Player player, int amount)
    {
        // +1 extra Qi per gain event, per stack of RusticRoot.
        return amount + Amount;
    }

    public Task AfterModifyingQiGain(
        PlayerChoiceContext ctx,
        Player player,
        int originalAmount,
        int modifiedAmount)
    {
        // No side effect on gain itself; the extra Qi was already added by ModifyQiGain.
        return Task.CompletedTask;
    }
}
