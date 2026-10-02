using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>龙怒：本回合受到来自奶龙的伤害翻倍（可叠加，单回合）。</summary>
/// <remarks>
///     挡灾 (联机卡) 给伊林自身挂上的负面状态：替全队挡下奶龙的刀。
///     与心防 (队友来自奶龙伤害减半) 对称 —— 一减一增，构成干净的联机攻守交易。
///     单回合：在自身回合开始前自动散去；作为 PowerType.Debuff 可被涤荡清除。
/// </remarks>
public sealed class LongNu : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Debuff;
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
            return 2m;
        return 1m;
    }

    /// <summary>
    ///     单回合过期 —— 同 <see cref="HeartGuard"/>: 必须用<b>玩家级</b>钩子而非 side 级钩子。
    ///     联机时所有玩家同属一个 <c>CombatSide</c>, <c>side == Owner.Side</c> 区分不出是谁的回合,
    ///     会把全队的龙怒一并抹掉。改用 <c>player == Owner.Player</c>, 只在本人回合开始时结算。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
            await PowerCmd.Remove(this);
        await base.AfterPlayerTurnStart(choiceContext, player);
    }
}
