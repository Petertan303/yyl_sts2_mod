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
///         设计意图: 产炁牌偏防御/功能, 温养把"获得炁"这个动作换算成即时防御。
///         ★2026-11-08: 丹噬改为失炁触发后, 两个收口分别落在产炁侧(格挡) 与耗炁侧(中毒);
///         卡牌给予 3 层, 层数即<b>倍率</b> (原 1 层)。
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

        // ★钩子遍历全场监听者, 联机时队友得炁不该由本 power 结算 (与行炁/丹噬同理)。
        if (player != Owner.Player) return;

        // ★格挡 = 本次获得的炁量 × 温养层数 (公式不变, 层数由卡牌给 3)。
        var block = (decimal)modifiedAmount * Amount;
        if (block <= 0) return;

        await CreatureCmd.GainBlock(player.Creature, block, ValueProp.Unpowered, null);
    }
}
