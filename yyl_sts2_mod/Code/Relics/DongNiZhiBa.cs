using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     东尼之霸（战斗内遗物）：你获得的格挡恒定为 15（任意来源 —— 卡牌 / 遗物 / buff）。
///     <para>
///         纯引擎原生接管：遗物直接覆写 <c>ModifyBlockMultiplicative</c>（与原版护腕
///         Vambrace 同管线），返回 15/当前值 —— 遗物在钩子枚举里排在所有 Power 之后
///         （Powers → Relics → Potions → Orbs → Cards），因此能纠正之前的一切修正
///         （如逆生一重的翻倍）。商向上补一位 1e-28，消除除不尽时的 decimal 尘埃
///         （否则 15/13*13 会在历史记录的 (int) 截断里变成 14）。
///         由同名卡牌打出时赋予，战斗结束自毁（<see cref="AfterCombatEnd" />）。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class DongNiZhiBa : yylRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner.Creature || block <= 0m)
            return 1m;

        var mult = 15m / block;
        if (block * mult < 15m)
            mult += 0.0000000000000000000000000001m; // 1e-28: 补齐 decimal 除法的向下舍入
        return mult;
    }

    /// <summary>战斗内遗物：战斗结束自毁（时效与被替换的 Power 版一致）。</summary>
    public override Task AfterCombatEnd(CombatRoom room)
    {
        RelicCmd.Remove(this);
        return Task.CompletedTask;
    }
}
