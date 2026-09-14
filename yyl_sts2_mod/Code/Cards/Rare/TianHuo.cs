using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Compatibility;
using yyl_sts2_mod.Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     天火: 1 费, 失去所有炁, 对所有敌人造成 失去值 × 14 → 18 伤害。
///     <para>
///         ⚠ 强清场: 10 炁时群伤 140 → 180。后续平衡时可能需要加"最多 5 炁"等限制。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class TianHuo(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public TianHuo() : this(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        var current = Owner.Creature.GetPower<Qi>()?.Amount ?? 0;
        if (current <= 0) return;

        var perDamage = 14; // TODO upgrade: would be 18 if IsUpgraded
        var totalDamage = perDamage * current;

        // 先走 LoseQi(让 DanShi 之类的 follow-up 触发)
        await yylCmd.LoseQi(choiceContext, Owner, current, this, cardPlay.Card);
        // 再群伤
        foreach (var enemy in combatState.HittableEnemies)
        {
            await CompatibilityCreatureCmd.Damage(
                choiceContext,
                enemy,
                totalDamage,
                default(ValueProp),
                cardSource: cardPlay.Card,
                cardPlay: cardPlay);
        }
    }
}
