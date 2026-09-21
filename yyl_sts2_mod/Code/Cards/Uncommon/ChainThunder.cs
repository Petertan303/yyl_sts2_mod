using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     连雷: 1 费, 对目标造成 8 → 11 伤害, 并随机对另一名敌人造成 4 → 5 伤害。
///     <para>
///         机制化 AoE (设计笔记 §6-①): 主目标吃全额, 溅射走随机目标,
///         区别于奶龙无影脚的直伤群攻。随机数用 RunState 的战斗 RNG
///         (与「运势」同一来源), 保证回放/多人一致性。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ChainThunder(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ChainThunder() : this(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
        // 溅射伤害变量: 独立升级 4 → 5。calc 恒 0 (无额外修正), 仅为声明变量供文案显示。
        WithCalculatedDamage("Splash", 4, (_, _) => 0m, (ValueProp)0, 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;

        // 主目标: 全额伤害 (与破防同款的显式数值攻击)。
        await CommonActions.CardAttack(this, cardPlay, target, DynamicVars.Damage.IntValue, ValueProp.Move)
            .WithHitFx("vfx/vfx_attack_lightning")
            .Execute(choiceContext);

        // 溅射: 随机挑另一名可命中敌人, 直接结算伤害。
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        var splashValue = DynamicVars["Splash"].IntValue;
        if (splashValue <= 0) return;
        var candidates = combatState.HittableEnemies
            .Where(e => e != null && e.IsHittable && e != target)
            .ToList();
        // 优先打另一名敌人; 场上只剩主目标时, 溅射回落到主目标身上 (单体时吃满两段)。
        var splashTarget = candidates.Count > 0
            ? Owner.RunState.Rng.CombatCardSelection.NextItem(candidates)
            : (target.IsHittable ? target : null);
        if (splashTarget == null) return;
        await CreatureCmd.Damage(choiceContext, splashTarget, (decimal)splashValue, ValueProp.Move,
            Owner.Creature, this, cardPlay);
    }
}
