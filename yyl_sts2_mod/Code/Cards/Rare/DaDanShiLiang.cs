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

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     大啖食粮: 1 费, 对所有奶龙造成 4 伤害, 回复等同造成伤害的生命。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class DaDanShiLiang(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public DaDanShiLiang() : this(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        var damage = 4; // TODO upgrade: would be 6 if IsUpgraded
        var totalHealed = 0m;
        foreach (var enemy in combatState.HittableEnemies)
        {
            if (!enemy.HasPower<NlPower>() && !enemy.HasPower<NlPowerPlus>()) continue;
            // 用真实伤害结果(返回 IEnumerable<DamageResult>),累加实际命中
            // TODO: 验证 DamageResult 的字段名(可能不叫 Amount, 可能是 TotalDamage / Damage / ActualDamage)
            var results = await CompatibilityCreatureCmd.Damage(
                choiceContext, enemy, damage, default(ValueProp), cardPlay.Card, cardPlay);
            // 暂时按预期伤害累加(因 DamageResult 字段名待确认)
            totalHealed += damage;
        }
        if (totalHealed > 0)
            await CreatureCmd.Heal(Owner.Creature, totalHealed);
    }
}

