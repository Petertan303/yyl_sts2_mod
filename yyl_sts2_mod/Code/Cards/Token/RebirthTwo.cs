using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Token;

/// <summary>
///     逆生二重: 3 费衍生牌 (由逆生一重加入手牌), 进入逆生二重姿态,
///     并把「逆生三重」加入手牌。消耗; 升级后添加保留。
/// </summary>
[Pool(typeof(TokenCardPool))]
public class RebirthTwo(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public RebirthTwo() : this(3, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithTip(typeof(RebirthThree));
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await StanceCmd.EnterReverseLife2(choiceContext, Owner, cardPlay.Card);
        await yylCmd.GiveCard<RebirthThree>(Owner, PileType.Hand, CardPilePosition.Random, upgraded: IsUpgraded);
    }
}
