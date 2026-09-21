using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using System.Linq;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     奶龙无影脚: 1 费, 对所有敌人造成 12 → 18 伤害, 并对所有生物 (含自己)
///     施加 4 → 2 层中毒。
///     <para>
///         直伤 AoE 位; 中毒无差别施放是"奶龙的踢击谁都遭殃"的代价。
///         [balance 2026-09-18] 移除格挡: 只保留伤害 + 自中毒负面。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public class NailongKick(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public NailongKick(): this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithDamage(12, 6);
        WithPower<PoisonPower>(4, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        
        // [balance] 移除格挡: 用户要求只保留伤害 + 自中毒负面, 不再提供格挡
        // var allCreatures = combatState.Enemies
        //     .Concat(combatState.GetTeammatesOf(Owner.Creature))
        //     .Append(Owner.Creature)
        //     .Where(c => c != null && c.IsAlive)
        //     .ToList(); 
        //
        // foreach (var creature in allCreatures)
        // {
        //     // await CommonActions.CardAttack(this, cardPlay)
        //     //     .Targeting(creature)
        //     //     .WithHitFx("vfx/vfx_attack_blunt")
        //     //     .Execute(choiceContext);
        //     // await CommonActions.CardAttack(this, cardPlay)
        //     //     .Targeting(creature)
        //     //     .Execute(choiceContext);
        //     // await PowerCmd.Apply<PoisonPower>(choiceContext, creature, 4, null, null);
        //     // await CommonActions.ApplySelf<PoisonPower>(choiceContext, this);
        // }
        
        await CommonActions.CardAttack(this, cardPlay)
            .Execute(choiceContext);
        await CommonActions.Apply<PoisonPower>(choiceContext, combatState.Creatures, this);
    }
}
