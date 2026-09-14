using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Compatibility;
using yyl_sts2_mod.Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     指认: 1 费, 造成 6 → 9 伤害, 若目标是奶龙, 获得 1 能量。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ZhiRen(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ZhiRen() : this(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = 6; // TODO upgrade: would be 9 if IsUpgraded
        var target = cardPlay.Target!;
        await CompatibilityCreatureCmd.Damage(
            choiceContext, target, damage, default(ValueProp), cardPlay.Card, cardPlay);
        // 若目标是奶龙, 获得 1 能量
        if (IsNailong(target))
        {
            await PlayerCmd.GainEnergy(1, Owner);
        }
    }

    private static bool IsNailong(MegaCrit.Sts2.Core.Entities.Creatures.Creature c) =>
        c.HasPower<NlPower>() || c.HasPower<NlPowerPlus>();
}

