using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Compatibility;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     狂热: 1 费, 对单体造成 4 伤害, 若目标是奶龙, 额外 +4 伤害。
///     <para>
///         原设计意图是"持有时, 攻击奶龙 +4"; 当前简化为"打出此牌时, 目标若是奶龙则 +4"。
///         若需要持久效果,把它改成 "Power 狂热(单层): 攻击奶龙 +4",卡牌施加 Power。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class KuangRe(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public KuangRe() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        var damage = 4 + (IsNailong(target) ? 4 : 0);
        await CompatibilityCreatureCmd.Damage(
            choiceContext, target, damage, default(ValueProp), cardPlay.Card, cardPlay);
    }

    private static bool IsNailong(MegaCrit.Sts2.Core.Entities.Creatures.Creature c) =>
        c.HasPower<NlPower>() || c.HasPower<NlPowerPlus>();
}
