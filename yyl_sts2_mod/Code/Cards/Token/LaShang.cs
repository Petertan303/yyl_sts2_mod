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
///     内伤: 状态牌。3 费可打出 (打出即清掉); 每个回合结束时若留在手里, 失去 2 点生命。
///     由「血雷」塞进手牌 —— 是攻击留下的后续账单, 玩家可花 3 能量主动清理。
///     2026-11-18 用户定调: 从不可打出 (-1 费) 改为 3 费, 给玩家一个"付费消灾"的选择。
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
    // ★2026-11-18: 原 -1 费 + Unplayable (原版感染/灼伤同款), 改为 3 费可打出 ——
    //   玩家可主动花 3 能量把这张账单牌打出, 避免回合末在手里失去 2 血。
    public LaShang() : this(3, CardType.Status, CardRarity.Status, TargetType.None)
    {
        // 每回合结束的自伤 (卡面用)
        WithCalculatedDamage("HpLoss", 2, (_, _) => 0m, 0, 0, 0);
    }

    // 原版状态牌均不可升级 (Infection/Burn/Wound 同款)
    public override int MaxUpgradeLevel => 0;

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // "失去生命"而非伤害: 不走伤害管线, 不吃炁/姿态等增减伤 (与燃炁同款修正)。
        yylCmd.LoseHp(Owner.Creature, DynamicVars["HpLoss"].IntValue);
    }
}
