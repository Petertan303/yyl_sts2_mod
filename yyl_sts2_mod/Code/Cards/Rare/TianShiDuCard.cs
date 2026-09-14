using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     天师度 卡牌: 1 费, 获得 10 炁 + 3 层金光咒, 同时给予 天师度 Power
///     (回合一结束 -2 炁 / 层)。类比 Wraith Form: 强力正向 + 持续负面。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class TianShiDuCard(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public TianShiDuCard() : this(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 正向: 获得 10 炁 + 3 层金光咒
        await yylCmd.GainQi(choiceContext, Owner, 10, this, cardPlay.Card);
        await PowerCmd.Apply<GoldenWave>(choiceContext, new[] { Owner.Creature }, 3, Owner.Creature, cardPlay.Card);
        // 负面: 给予天师度 debuff Power
        await PowerCmd.Apply<TianShiDu>(choiceContext, new[] { Owner.Creature }, 1, Owner.Creature, cardPlay.Card);
    }
}
