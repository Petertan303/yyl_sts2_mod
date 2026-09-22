using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>戮龙印：持有者攻击奶龙时造成的伤害翻倍。</summary>
/// <remarks>
///     共同狩猎 (联机卡) 给指定队友挂上的增益：伊林用黄桃罐头把敌人标成奶龙，
///     队友打它们即翻倍 —— 伊林"指认"、队友"收割"的联机闭环。
///     修正接口挂在攻击者 (持有者) 上，仅对带奶龙标记的目标生效。
/// </remarks>
public sealed class LuLongYin : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer == Owner && target != null && !props.HasFlag(ValueProp.Unpowered) && yylNailong.IsNailongMarked(target))
            return 2m;
        return 1m;
    }
}
