using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     养龙 (稀有遗物): 每击杀一个[奶龙], 本遗物 +1 层 (最多 10 层);
///     每层使你对[奶龙]造成的伤害 +1。
///     <para>
///         跨战斗累计的成长型遗物 —— 起始遗物「黄桃罐头」会把所有敌人视作奶龙,
///         所以层数实际上就是"这场旅途中累计击杀数", 属于局外成长。
///     因为计数器是永久的, 上限必须存在, 否则后期会变成一回合秒杀。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class NailongRearing : yylRelicModel, IModifyDamageAdditive
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    /// <summary>层数上限。</summary>
    public const int MaxStacks = 10;

    public override bool IsStackable => true;

    public override bool ShowCounter => true;

    public override Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented) return Task.CompletedTask;
        if (!yylNailong.IsNailong(creature)) return Task.CompletedTask;
        if (StackCount >= MaxStacks) return Task.CompletedTask;

        IncrementStackCount();
        Flash();
        return Task.CompletedTask;
    }

    public decimal ModifyDamageAdditiveCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner.Creature) return 0m;
        if (props.HasFlag(ValueProp.Unpowered)) return 0m;
        if (!yylNailong.IsNailong(target)) return 0m;

        return Math.Min(StackCount, MaxStacks);
    }
}
