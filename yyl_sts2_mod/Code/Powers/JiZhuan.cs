using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     积砖: 你的回合开始时 (格挡被引擎清空的时刻), 获得等同 <c>DynamicVars.Block</c> 的格挡,
///     然后层数 -1; 减到 0 自动散去。
///     <para>
///         ★逻辑完全照原版 <c>ToricToughnessPower</c> ("在下 2 个回合开始时获得 5 点格挡",
///         反编译确认: 覆写 <c>AfterBlockCleared</c> → Flash → GainBlock(DynamicVars.Block) → Decrement)。
///         与原版的唯一差异: 每回合格挡数量由给予方通过 <see cref="SetBlock" /> 参数化 (原版固定 5)。
///     </para>
///     <para>
///         ★必须覆写 <see cref="CanonicalVars" /> 注册 "Block" 变量 (原版同款:
///         <c>new BlockVar(基值, ValueProp.Unpowered)</c>)。缺注册时 <see cref="SetBlock" /> 的
///         <c>DynamicVars.Block</c> 与描述的 <c>{Block:diff()}</c> 都会 KeyNotFoundException →
///         PlayCardAction 失败 → 打出卡牌直接卡死 (2026-11-04 实测)。
///     </para>
///     <para>
///         由「磨镜成砖」给予 (Counter = 2 回合, 每回合 8 → 12 点)。
///         描述用 {Block} 动态变量, 自定义数值后悬浮提示自动正确。
///     </para>
/// </summary>
public sealed class JiZhuan : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 注册 "Block" 变量 (默认 8 = 未升级值; 实际值由磨镜成砖 OnPlay 的 SetBlock 覆盖)。
    // Unpowered 标记照抄原版 ToricToughnessPower —— 该数值不受其它 power 加成影响。
    protected override IReadOnlyList<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(8m, ValueProp.Unpowered) };

    /// <summary>设置每回合开始时获得的格挡数 (照原版 SetBlock; 原版固定 5, 此处参数化)。</summary>
    public void SetBlock(decimal value)
    {
        AssertMutable();
        DynamicVars.Block.BaseValue = value;
    }

    public override async Task AfterBlockCleared(Creature creature)
    {
        if (creature != Owner) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, DynamicVars.Block, null, false);
        // 层数 -1: 照抄原版 ToricToughnessPower —— `PowerCmd.Decrement(power)` (公开静态方法,
        //   内部即 await PowerCmd.ModifyAmount(power, -1m, null, null, false))。走命令管线,
        //   联机同步且层数归 0 时正确散去。
        //   ⚠ 不能用 SetAmount(Amount-1): 那只改数字, 0 层不移除, 会继续 0 → -1 → -2 无限触发
        //     (2026-11-04 实测; 此前"Decrement 是引擎 internal 不可见"的结论是误判)。
        await PowerCmd.Decrement(this);
    }
}
