using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     清心: 持有者本回合受到的负面状态无效 —— 新挂的 Debuff 获得量被改写为 0,
///     但不清除已有层数。回合结束 (敌方回合收尾) 自动移除。
///     <para>
///         实现仿原版圣物「Artifact」: 覆写 <c>TryModifyPowerAmountReceived</c>,
///         把 Debuff 的获得量改为 0。与 Artifact 不同点: 不消耗自身层数,
///         到点整个散去 (由「清心咒」打出, 单层 Counter)。
///     </para>
/// </summary>
public sealed class PurityVeil : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    ///     负面状态 (Debuff) 的获得量改写为 0; 返回 true 表示"已改写, 用 modified 值"。
    ///     Buff 与无关 Power 原样放行。
    /// </summary>
    public override bool TryModifyPowerAmountReceived(
        PowerModel power,
        Creature receiver,
        decimal amount,
        Creature giver,
        out decimal modified)
    {
        modified = amount;
        if (Amount <= 0) return false;
        if (power.Type != PowerType.Debuff) return false;
        modified = 0m;
        return true;
    }

    /// <summary>敌方回合收尾时散去 (保护窗口 = 打出后的本回合 + 敌方回合)。</summary>
    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext ctx,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Side) return Task.CompletedTask;
        if (Amount <= 0) return Task.CompletedTask;
        return PowerCmd.Remove(this);
    }
}
