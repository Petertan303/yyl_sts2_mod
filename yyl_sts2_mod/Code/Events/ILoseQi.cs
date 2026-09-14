using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace yyl_sts2_mod.Code.Events;

/// <summary>
///     Hook for models (powers, relics, stances, ...) that react when the player loses Qi.
///     Listeners are invoked in hook-listener order via <see cref="yylHook.ModifyQiLoss" />
///     and the resulting (possibly-modified) loss is applied to <c>Qi</c> via
///     <see cref="yylCmd.LoseQi" />.
/// </summary>
public interface ILoseQi
{
    /// <summary>
    ///     Returns the adjusted Qi loss. Called once per Qi loss event, receiving the value as
    ///     modified by any earlier listeners.
    ///     <para>
    ///         Return <paramref name="amount" /> unchanged when this listener only needs the
    ///         <see cref="AfterModifyingQiLoss" /> follow-up.
    ///     </para>
    ///     <para>
    ///         The return value is interpreted as a positive number (amount of Qi to remove);
    ///         the actual removal will be clamped to the current Qi amount on the target.
    ///     </para>
    /// </summary>
    int ModifyQiLoss(Player player, int amount);

    /// <summary>
    ///     Follow-up invoked after the final Qi change has been applied. Use this for side
    ///     effects such as gaining block or consuming charges.
    /// </summary>
    Task AfterModifyingQiLoss(PlayerChoiceContext ctx, Player player, int originalAmount, int modifiedAmount);
}
