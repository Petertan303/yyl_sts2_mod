using MegaCrit.Sts2.Core.Entities.Powers;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     祖炁形态 —— 伊林的<b>形态</b>（对标原版恶魔/幽魂/天人等「xx形态」卡）。
///     持有期间，炁的增伤不再收益递减，改为<b>线性</b>：每 1 点炁提高伤害
///     <c>Amount</c>%（基础 8%，升级 12%）。
///     <para>
///         原版形态卡的共同特征：名字叫「xx形态」、3 费、<b>升级不降费</b>（改为增强效果）。
///         本卡同样 3 费且不降费，升级只把系数 8% → 12%。
///     </para>
///     <para>
///         本 Power 自身不参与伤害计算，只作为「公式开关 + 系数载体」被
///         <see cref="Qi.ModifyDamageMultiplicativeCompability" /> 读取。
///         与「守势」的优先级：守势 &gt; 祖炁形态 &gt; 默认对数曲线（守势会完全取消炁增伤）。
///     </para>
/// </summary>
public sealed class ZuQiForm : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>层数 = 每 1 点炁提供的伤害加成百分比（8 → 12）。形态本身不叠加。</summary>
    public override PowerStackType StackType => PowerStackType.Counter;
}
