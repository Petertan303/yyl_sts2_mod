using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace yyl_sts2_mod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class ReverseLife3(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ReverseLife3() : this(3, CardType.Power, CardRarity.Token, TargetType.None)
    {
        WithPower<Powers.ReverseLife>(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
    {
        await CommonActions.ApplySelf<Powers.ReverseLife>(ctx, this);
    }
}