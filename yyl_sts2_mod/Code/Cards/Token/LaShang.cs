using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Token;

/// <summary>
///     拉伤: 状态牌。不可打出; 每个回合结束时 (留在手里) 失去 2 点生命。
///     由「血雷」塞进弃牌堆 —— 是高数值攻击留下的后续账单。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class LaShang(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public LaShang() : this(0, CardType.Status, CardRarity.Status, TargetType.None)
    {
        WithKeywords(CardKeyword.Unplayable);
        // 每回合结束的自伤 (卡面用)
        WithCalculatedDamage("HpLoss", 2, (_, _) => 0m, 0, 0, 0);
    }

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // "失去生命"而非伤害: 不走伤害管线, 不吃炁/姿态等增减伤 (与燃炁同款修正)。
        yylCmd.LoseHp(Owner.Creature, DynamicVars["HpLoss"].IntValue);
    }
}
