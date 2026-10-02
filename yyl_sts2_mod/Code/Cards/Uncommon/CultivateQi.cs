using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     养炁: 1 费(升级 0 费)罕见能力牌, 获得 1 层[养炁] —— 获得炁时每层额外获得 1 点炁。
///     <para>
///         与「温养」(获得炁→格挡) 同源互补: 养炁放大"获得炁"本身,
///         让后续温养 / 丹噬的倍率更高。养炁层数即"每次获得炁多给几炁"。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class CultivateQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public CultivateQi() : this(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        // 2 → 1 费 (升级 -1); 获得 1 层养炁 (每层使后续获得炁 +1)。
        WithCostUpgradeBy(-1);
        WithPower<Powers.CultivateQi>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var amount = DynamicVars[typeof(Powers.CultivateQi).Name].IntValue;
        await PowerCmd.Apply<Powers.CultivateQi>(
            choiceContext,
            new[] { Owner.Creature },
            amount,
            Owner.Creature,
            cardPlay.Card);
    }
}
