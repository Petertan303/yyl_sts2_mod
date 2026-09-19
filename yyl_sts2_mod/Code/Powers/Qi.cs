using System;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;

namespace yyl_sts2_mod.Code.Powers;

public sealed class Qi : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    ///     对数增伤系数: bonus(stack) = LogCoeff * ln(1 + stack), 无硬上限, 前期收益高、后期平缓。
    ///     参考点: 1炁≈+19%(1.19x), 10炁≈+65%(1.65x), 20炁≈+83%(1.83x), 40炁≈+100%(2.0x)。
    ///     这样炁"道中有用"(前期每点都有感), 也"能作为上限"(后期仍涨但不爆炸)。
    /// </summary>
    public const decimal LogCoeff = 0.27m;

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer == Owner && !props.HasFlag(ValueProp.Unpowered))
        {
            // 「守势」: 把炁的攻击收益换成金光的防御收益 —— 有守势时炁不提供增伤。
            if (Owner.HasPower<ShouShi>()) return 1m;
            if (Amount <= 0) return 1m;
            decimal bonus = LogCoeff * (decimal)Math.Log(1.0 + Amount);
            return 1m + bonus;
        }
        return 1m;
    }
}
