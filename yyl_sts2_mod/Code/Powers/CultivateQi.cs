using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     养炁: <b>获得炁时</b>, 每层额外获得 1 点炁(即 "获得 X 炁" 实际变为 X + 层数)。
///     <para>
///         设计意图: 养炁放大"获得炁"这个动作本身 —— 让后续温养(炁→格挡) / 丹噬(炁→伤害)
///         的倍率更高。与「温养」(获得炁→格挡) 形成攻防两条收口引擎的源头增益。
///     </para>
///     <para>
///         Hook-based: 实现 <see cref="IGainQi" />, 在 ModifyQiGain 阶段直接把增益叠进炁量,
///         正常参与炁获取链与任何其它未来炁增益修正。
///     </para>
/// </summary>
public sealed class CultivateQi : yylPowerModel, IGainQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int ModifyQiGain(Player player, int amount)
    {
        // +1 extra Qi per gain event, per stack of CultivateQi.
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
