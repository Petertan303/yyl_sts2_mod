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
    ///     <para>
    ///         ★★[2026-10-15 修正] 必须判 <c>receiver == Owner</c>。
    ///         反汇编确认: 引擎在 <c>Hooks.Hook.ModifyPowerAmountReceived</c> 里是
    ///         用 <c>IterateCombatHookListeners</c> 遍历<b>整场战斗所有监听者</b>来调用
    ///         本方法的 —— 它不会因为"这次 debuff 是挂在别人身上"就跳过我们。
    ///         漏掉 receiver 判定的后果: 只要队伍里<b>任何一人</b>带着清心,
    ///         全队 (甚至敌人施加给任何人的 debuff) 都会被一并无效化,
    ///         与卡面"本回合你受到的负面状态无效"完全不符。
    ///     </para>
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
        // 只保护持有者本人 (上面的遍历语义说明: 不判就会变成"对 debuff 本身生效")。
        if (receiver != Owner) return false;
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
