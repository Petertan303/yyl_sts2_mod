using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     祖炁形态 —— 伊林的<b>形态</b>（对标原版恶魔/幽魂/天人等「xx形态」卡）。
///     持有期间，炁的增伤不再分段衰减，改为<b>单段线性</b>：每 1 点炁提高伤害
///     <c>Amount</c>%（2026-11-15 用户定调：基础 9%，升级 12.5%）。
///     <para>
///         原版形态卡的共同特征：名字叫「xx形态」、3 费、<b>升级不降费</b>（改为增强效果）。
///         本卡同样 3 费且不降费，升级只把系数 9% → 12.5%。
///     </para>
///     <para>
///         本 Power 自身不参与伤害计算，只作为「公式开关 + 系数载体」被
///         <see cref="Qi.ModifyDamageMultiplicativeCompability" /> 读取。
///         与「守势」的优先级：守势 &gt; 祖炁形态 &gt; 默认两段曲线（守势会完全取消炁增伤）。
///         默认曲线 8 炁前 12.5%/点、之后 4%/点；形态把 12.5% 附近的高斜率一直延伸下去。
///     </para>
/// </summary>
public sealed class ZuQiForm : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>层数 = 每 1 点炁提供的伤害加成百分比（9 → 12.5，允许小数）。形态本身不叠加。</summary>
    public override PowerStackType StackType => PowerStackType.Counter;

    // ── 特效刷新: 施加 / 移除时切换 "气旋提成光柱" 的反向 tween (纯视觉层) ──
    public override Task AfterApplied(Creature target, CardModel source)
    {
        try
        {
            QiVfx.Refresh(target ?? Owner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 祖炁vfx AfterApplied 失败: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature target)
    {
        try
        {
            QiVfx.Refresh(target ?? Owner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 祖炁vfx AfterRemoved 失败: {ex.Message}");
        }
        QiVfx.Unsubscribe(this);
        return Task.CompletedTask;
    }
}
