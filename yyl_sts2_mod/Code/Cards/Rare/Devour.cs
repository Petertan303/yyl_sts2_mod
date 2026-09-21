using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     大啖食粮: 2 费, 对所有奶龙造成 4 → 6 伤害, 回复等同造成伤害的生命。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Devour(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Devour() : this(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(4, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        var totalHealed = 0m;
        // 只打非队友的奶龙: 联机时队友也可能被起始遗物标记为奶龙, 误伤队友不可接受。
        var teammates = combatState.GetTeammatesOf(Owner.Creature).ToHashSet();
        foreach (var enemy in combatState.HittableEnemies)
        {
            if (!yylNailong.IsNailong(enemy)) continue;
            if (teammates.Contains(enemy)) continue;
            var attack = await CommonActions.CardAttack(this, cardPlay, enemy, DynamicVars.Damage.IntValue,
                    ValueProp.Move)
                .WithHitFx("vfx/vfx_bite") // 打出特效: 原版撕咬 (照搬)
                .Execute(choiceContext);
            totalHealed += attack.Results.SelectMany(result => result).Sum(result => result.UnblockedDamage);
        }
        if (totalHealed > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, totalHealed);
            yylVfx.OnCreature(Owner.Creature, "vfx/vfx_cross_heal"); // 吸食回复特效
        }
    }
}
