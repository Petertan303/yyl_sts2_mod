using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     诛邪: 2 费, 造成 9 → 13 伤害。若目标因此死亡, 对所有其他生命值不高于 50%
///     的敌人造成其当前生命值的伤害 (无视格挡, 即连斩)。
///     <para>
///         机制化 AoE / 斩杀 (设计笔记 §6-③): 参考观者「审判」的执行思路。
///         连斩伤害直接取当前生命值并带 Unblockable, 保证无视格挡必死;
///         走正常伤害管线 (而非 CreatureCmd.Kill), 保留死亡钩子与动画。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ZhuXie(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    /// <summary>连斩的血量阈值: 最大生命值占比 ≤ 50%。</summary>
    public const double ExecuteThreshold = 0.5;

    public ZhuXie() : this(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(9, 4);
        // 连斩的血量阈值 (百分比, 卡面用)
        WithCalculatedDamage("Threshold", 50, (_, _) => 0m, 0, 0, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;

        await CommonActions.CardAttack(this, cardPlay, target, DynamicVars.Damage.IntValue, ValueProp.Move)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 目标被斩杀后, 连斩其他低血量敌人。
        if (!target.IsDead) return;
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        foreach (var enemy in combatState.HittableEnemies)
        {
            if (enemy == null || enemy == target || !enemy.IsHittable) continue;
            if (enemy.GetHpPercentRemaining() > DynamicVars["Threshold"].IntValue / 100.0) continue;
            // 直接取当前生命值 + Unblockable: 无视格挡, 必定致命。
            await CreatureCmd.Damage(choiceContext, enemy, enemy.CurrentHp, ValueProp.Unblockable,
                Owner.Creature, this, cardPlay);
        }
    }
}
