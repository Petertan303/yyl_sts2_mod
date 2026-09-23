using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     天火: 2 费, 失去所有炁, 对所有敌人造成 失去值 × 8 → 12 伤害。
///     [rule 2026-09-18] 炁不足时无额外效果 (不打出一滴伤害)。
///     <para>
///         ⚠ 强清场: 10 炁时群伤 80 → 120; 但炁伤害乘区已改对数,
///         大量囤炁的直接收益主要就体现在这张卡上。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class SkyFire(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public SkyFire() : this(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(8, 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        var current = Owner.Creature.GetPower<Qi>()?.Amount ?? 0;
        if (current <= 0) return;

        var totalDamage = DynamicVars.Damage.IntValue * current;

        /*  ★2026-09-22 修复: 实际伤害比卡面预览少。
            原先先 LoseQi 再群伤 —— 炁被清空后, 伤害管线里「炁」的增伤乘区
            (Qi.ModifyDamageMultiplicativeCompability, 0.27·ln(1+炁)) 也跟着归零,
            于是打出来的数字低于悬停时看到的预览值。
            改为**先结算伤害**(此时炁还在, 乘区与预览同源), 再失去所有炁。
            失去炁的后续 (温养给格挡 / 行炁抽牌 等 ILoseQi 订阅) 仍然照常触发,
            只是排到了伤害之后, 不影响本卡结算。 */
        foreach (var enemy in combatState.HittableEnemies)
        {
            await CommonActions.CardAttack(this, cardPlay, enemy, totalDamage, ValueProp.Move)
                .WithHitFx("vfx/vfx_fire_burst")
                .Execute(choiceContext);
        }
        // 伤害打完再清空炁
        await yylCmd.LoseQi(choiceContext, Owner, current, this, cardPlay.Card);
    }
}
