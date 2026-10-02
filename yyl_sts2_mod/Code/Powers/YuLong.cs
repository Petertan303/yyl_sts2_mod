using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     驭龙: 仿原版 <b>TRACKING（追踪）</b> —— 原版条件是「虚弱」，这里改为「奶龙」。
///     你造成的攻击对[gold]奶龙[/gold]的伤害提高 <c>Amount</c>%（基础 50%）。
///     <para>
///         与「狂热」（<see cref="Fervor" />，每层对奶龙<b>加法</b> +2 伤）互补：
///         狂热是加法、驭龙是乘法，两者可叠加，共同撑起奶龙流的加成天花板。
///         守卫条件与狂热完全一致（dealer == Owner / 非 Unpowered / 目标为奶龙），
///         保证联机下只对自己的伤害生效。
///     </para>
/// </summary>
public sealed class YuLong : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>层数 = 对奶龙的伤害加成百分比。</summary>
    public override PowerStackType StackType => PowerStackType.Counter;

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        if (!yylNailong.IsNailong(target)) return 1m;
        return 1m + (decimal)Amount / 100m;
    }
}
