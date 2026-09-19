using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Token;

/// <summary>
///     逆生三重: 3 费衍生牌 (由逆生二重加入手牌), 进入逆生三重姿态。
///     姿态链终点; 升级后添加保留。
/// </summary>
[Pool(typeof(TokenCardPool))]
public class RebirthThree(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public RebirthThree() : this(3, CardType.Power, CardRarity.Token, TargetType.None)
    {
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await StanceCmd.EnterReverseLife3(ctx, Owner, cardPlay.Card);
    }
}
