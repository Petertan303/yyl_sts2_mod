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
///     狂热: 持有者攻击奶龙时, 每层额外造成 4 点伤害。
///     与「破损的奶龙玩偶」(减伤)分属攻防两端,互不影响。
/// </summary>
public sealed class Fervor : yylPowerModel, IModifyDamageAdditive
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>每层对奶龙目标的额外伤害。</summary>
    public const int BonusDamage = 2;

    public decimal ModifyDamageAdditiveCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner) return 0m;
        if (props.HasFlag(ValueProp.Unpowered)) return 0m;
        if (!yylNailong.IsNailong(target)) return 0m;
        return BonusDamage * Amount;
    }
}
