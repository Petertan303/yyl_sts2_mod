using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Multiplayer;

/// <summary>
///     共同狩猎: 3 费联机卡 (稀有)，消耗。指定一名队友，该队友攻击奶龙时造成的伤害翻倍 (戮龙印)。
///     <para>
///         联机专属 (TargetType.AnyAlly)。伊林用黄桃罐头把敌人标成奶龙，队友打它们即翻倍 ——
///         "指认 + 收割"的联机闭环。高费 + 消耗卡住稀有强度。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class GongTongShouLie(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    // 原版联机卡同款标记 (Sneaky/Fade/Coordinate 等): 单人局不入卡池
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public GongTongShouLie() : this(3, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is { IsAlive: true })
            await PowerCmd.Apply<Powers.LuLongYin>(choiceContext, new[] { cardPlay.Target }, 1m, Owner.Creature, cardPlay.Card);
    }
}
