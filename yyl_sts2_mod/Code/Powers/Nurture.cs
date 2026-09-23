using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     温养: <b>获得炁时</b>, 获得「本次获得的炁量 × 温养层数」点格挡。
///     <para>
///         设计意图: 产炁牌偏防御, 缺输出 —— 温养把"获得炁"这个动作换成格挡,
///         让防转续航。与「丹噬」(获得炁→伤害) 正好互补: 丹噬把炁换算成输出,
///         温养把炁换算成防御。层数即<b>倍率</b>, 攒一大笔炁再吃下去才是正确用法。
///     </para>
///     <para>
///         Hook-based: 实现 <see cref="IGainQi" />, 在每次获得炁后作为后续副作用结算。
///     </para>
/// </summary>
public sealed class Nurture : yylPowerModel, IGainQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int ModifyQiGain(Player player, int amount)
    {
        // 不参与数值修改, 只挂 AfterModifyingQiGain 后续结算。
        return amount;
    }

    public async Task AfterModifyingQiGain(
        PlayerChoiceContext ctx,
        Player player,
        int originalAmount,
        int modifiedAmount)
    {
        if (modifiedAmount <= 0) return;

        // ★格挡 = 本次获得的炁量 × 温养层数 (2026-09-23 改, 与丹噬对称)。
        var block = (decimal)modifiedAmount * Amount;
        if (block <= 0) return;

        await CreatureCmd.GainBlock(player.Creature, block, ValueProp.Unpowered, null);
    }
}
