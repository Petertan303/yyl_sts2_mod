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
///     养炁: 2 费(升级 1 费)罕见能力牌, 获得 1 层[温养] —— 获得炁时获得 (获得炁量 × 温养层数) 格挡。
///     <para>
///         与「丹噬」(获得炁→伤害) 互补: 丹噬把炁换算成输出, 养炁把炁换算成防御。
///         温养层数即倍率, 攒一大笔炁再吃下去才是正确用法。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class Nurture(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Nurture() : this(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        // 2 → 1 费 (升级 -1); 获得 1 层温养 (数值为倍率, 不随升级叠加层数)。
        WithCostUpgradeBy(-1);
        WithPower<Powers.Nurture>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var amount = DynamicVars[typeof(Powers.Nurture).Name].IntValue;
        await PowerCmd.Apply<Powers.Nurture>(
            choiceContext,
            new[] { Owner.Creature },
            amount,
            Owner.Creature,
            cardPlay.Card);
    }
}
