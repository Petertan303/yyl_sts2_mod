using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     丹噬: 失去炁时, 获得 4 → 6 点格挡 / 层。
///     Hook-based: implements <see cref="ILoseQi" />, reacts as a follow-up side effect
///     whenever the player loses Qi.
/// </summary>
public sealed class CinnabarBite : yylPowerModel, ILoseQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>Base block gained per stack of CinnabarBite on each Qi loss event.</summary>
    public const int BaseBlock = 4;

    public int ModifyQiLoss(Player player, int amount)
    {
        // Opt out of the modify pass — we only care about the after-modifying follow-up.
        return amount;
    }

    public async Task AfterModifyingQiLoss(
        PlayerChoiceContext ctx,
        Player player,
        int originalAmount,
        int modifiedAmount)
    {
        if (modifiedAmount <= 0) return;
        var block = BaseBlock * Amount;
        if (block <= 0) return;

        await CreatureCmd.GainBlock(player.Creature, block, ValueProp.Unpowered, null);
    }
}
