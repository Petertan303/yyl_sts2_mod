using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Cards.Token;

/// <summary>
///     凋萎: 状态牌。不可打出; 每个回合结束时 (留在手里) 失去 2 点生命。
///     由「血雷」塞进弃牌堆 —— 是高数值攻击留下的后续账单。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class Wilt(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Wilt() : this(0, CardType.Status, CardRarity.Status, TargetType.None)
    {
        WithKeywords(CardKeyword.Unplayable);
    }

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 2m,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }
}
