using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     温养: <b>失去炁时</b>, 获得 4 → 6 点格挡。
///     <para>
///         设计意图: 耗炁牌偏攻击与运转, 血量压力大 —— 温养把"失去炁"这个代价换成格挡,
///         让攻转防、续航。层数即格挡值(同原版「荆棘」的写法), 多张温养叠加。
///     </para>
///     <para>
///         Hook-based: 实现 <see cref="ILoseQi" />, 在每次失去炁后作为后续副作用结算。
///     </para>
/// </summary>
public sealed class Nurture : yylPowerModel, ILoseQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>卡牌默认给予的数值 (升级后 6)。</summary>
    public const int DefaultAmount = 4;

    /// <summary>升级时增加的数值。</summary>
    public const int UpgradeAmount = 2;

    public int ModifyQiLoss(Player player, int amount)
    {
        // 不参与数值修改, 只挂 AfterModifyingQiLoss 后续结算。
        return amount;
    }

    public async Task AfterModifyingQiLoss(
        PlayerChoiceContext ctx,
        Player player,
        int originalAmount,
        int modifiedAmount)
    {
        if (modifiedAmount <= 0) return;

        var block = Amount;
        if (block <= 0) return;

        await CreatureCmd.GainBlock(player.Creature, block, ValueProp.Unpowered, null);
    }
}
