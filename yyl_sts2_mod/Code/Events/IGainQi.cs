using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace yyl_sts2_mod.Code.Events;

/// <summary>
///     Hook for models (powers, relics, stances, ...) that react when the player gains Qi.
///     Listeners are invoked in hook-listener order via <see cref="yylHook.ModifyQiGain" />
///     and the resulting (possibly-modified) amount is applied to <c>Qi</c> via
///     <see cref="yylCmd.GainQi" />.
/// </summary>
public interface IGainQi
{
    /// <summary>
    ///     Returns the adjusted Qi gain. Called once per Qi gain event, receiving the value as
    ///     modified by any earlier listeners.
    ///     <para>
    ///         Return <paramref name="amount" /> unchanged when this listener only needs the
    ///         <see cref="AfterModifyingQiGain" /> follow-up.
    ///     </para>
    /// </summary>
    int ModifyQiGain(Player player, int amount);

    /// <summary>
    ///     Follow-up invoked after the final Qi change has been applied. Use this for side
    ///     effects such as dealing damage, sounds, or consuming charges.
    ///     <para>
    ///         <paramref name="modifiedAmount" /> is the final amount after all listeners; it
    ///         may be 0 (no Qi actually gained) — do not assume a positive amount.
    ///     </para>
    /// </summary>
    Task AfterModifyingQiGain(PlayerChoiceContext ctx, Player player, int originalAmount, int modifiedAmount);
}
