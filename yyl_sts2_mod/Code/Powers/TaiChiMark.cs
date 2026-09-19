using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     太极: 持有者受到的伤害转移给一名随机敌人 (借力打力)。
///     重定向本体在 <see cref="Patches.TaiChiRedirectPatch" /> 的 Harmony 前缀里;
///     本 Power 只作"开关 + 剩余回合"标记, 回合结束 (敌方回合收尾) 自动移除。
///     <para>
///         由「太极」卡打出 (消耗 2 炁), 单层 Counter。移除时机与
///         <see cref="PurityVeil" /> 一致: 保护窗口 = 打出后的本回合 + 敌方回合。
///     </para>
/// </summary>
public sealed class TaiChiMark : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>敌方回合收尾时散去 (保证敌方回合内的攻击已被转移)。</summary>
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
