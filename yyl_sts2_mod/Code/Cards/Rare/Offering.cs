using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     投喂: 2 费, 给予所有奶龙 10 点格挡, 按奶龙数量每只获得 4 → 6 点炁, 获得 2 点能量。
///     <para>
///         [rework 2026-09-19] 用户定调: 回归"给奶龙加格挡 + 产炁 + 能量"的原版定位,
///         产炁改为**按奶龙数量乘算** (每只 4 → 6) —— 敌群战大赚, Boss 战只算一只;
///         联机时队友也算奶龙, 支援属性保留。格挡只要挂上就计数 (不存在"回不上血"的空窗)。
///         与黑色幽默区分: 黑色幽默只回非队友奶龙且产炁固定。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Offering(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Offering() : this(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithBlock(10, 0);
        // 每只奶龙产出的炁 (升级 4 → 6)
        WithPower<Qi>("QiPer", 4, 2);
        WithEnergy(2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 给所有奶龙 10 格挡 (含队友; 单机时 = 给敌人上格挡, 这就是代价)。
        var count = 0;
        foreach (var c in combatState.Creatures)
        {
            if (!yylNailong.IsNailong(c)) continue;
            count++;
            await CreatureCmd.GainBlock(c, DynamicVars.Block.IntValue, ValueProp.Move, cardPlay);
        }

        // 按奶龙数量产炁: 每只 4 → 6 点。
        if (count > 0)
            await yylCmd.GainQi(choiceContext, Owner, count * DynamicVars["QiPer"].IntValue, this, cardPlay.Card);

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
