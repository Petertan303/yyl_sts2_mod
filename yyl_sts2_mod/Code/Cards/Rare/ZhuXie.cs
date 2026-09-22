using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     诛邪: 2 费, 造成 9 → 13 伤害。若目标因此死亡, 斩杀其他敌人中**生命值最低**
///     的那一个 (其当前生命值不高于 {Threshold} 点, 无视格挡)。
///     <para>
///         [balance 2026-09-19] 用户定调: 斩杀线由"最大生命值 50%"改为**定值 40 点**,
///         且由"连斩所有达线者"改为"只斩最脆的一个"。
///         斩杀仍走伤害管线 (取当前生命值 + Unblockable), 保留死亡钩子与动画。
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
    public ZhuXie() : this(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(9, 4);
        // 斩杀线 (定值生命, 卡面用; 升级不变)
        WithCalculatedDamage("Threshold", 40, (_, _) => 0m, 0, 0, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;

        await CommonActions.CardAttack(this, cardPlay, target, DynamicVars.Damage.IntValue, ValueProp.Move)
            .WithHitFx("vfx/vfx_dramatic_stab")
            .Execute(choiceContext);

        // 目标被斩杀后, 处决其他敌人中生命值最低且达线 (≤ {Threshold} 点) 的那一个。
        if (!target.IsDead) return;
        yylVfx.GrandFinaleImpact(target); // 斩杀演出: 华丽收场冲击
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        var threshold = DynamicVars["Threshold"].IntValue;
        Creature weakest = null;
        foreach (var enemy in combatState.HittableEnemies)
        {
            if (enemy == null || enemy == target || !enemy.IsHittable) continue;
            if (enemy.CurrentHp > threshold) continue;
            if (weakest == null || enemy.CurrentHp < weakest.CurrentHp)
                weakest = enemy;
        }

        if (weakest == null) return;
        // 直接取当前生命值 + Unblockable: 无视格挡, 必定致命。
        await CreatureCmd.Damage(choiceContext, weakest, weakest.CurrentHp, ValueProp.Unblockable,
            Owner.Creature, this, cardPlay);
        yylVfx.GrandFinaleImpact(weakest); // 连斩演出
    }
}
