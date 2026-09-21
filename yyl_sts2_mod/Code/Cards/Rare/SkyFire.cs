using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;
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

        // 先走 LoseQi(让 CinnabarBite 之类的 follow-up 触发)
        await yylCmd.LoseQi(choiceContext, Owner, current, this, cardPlay.Card);
        // 炁越足, 齐射光束越密 (上限 12 防过糊)。
        yylVfx.KinBeamColumn(Owner.Creature, Math.Min(current, 12), flipX: true);
        // 再群伤
        foreach (var enemy in combatState.HittableEnemies)
        {
            await CommonActions.CardAttack(this, cardPlay, enemy, totalDamage, ValueProp.Move)
                .WithHitFx("vfx/vfx_fire_burst")
                .Execute(choiceContext);
        }
    }
}
