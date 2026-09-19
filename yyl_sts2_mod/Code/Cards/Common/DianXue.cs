using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     点穴: 1 费技能, 给予 1 名敌人 8 → 12 层[点穴]; 然后所有敌人失去与
///     **自己身上**点穴层数相等的生命 (无视格挡)。
///     <para>
///         [balance 2026-09-19] 用户定调: 由全体施加改为单体施加 (技能),
///         但结算仍是全场按各自层数掉血 —— 层数留在敌人身上累积,
///         反复点同一个敌人会让它的"旧账"越滚越痛。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class DianXue(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public DianXue() : this(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<Powers.DianXue>(8, 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 只给攻击目标上点穴。
        var target = cardPlay.Target!;
        if (target == null) return;
        await CommonActions.Apply<Powers.DianXue>(choiceContext, new[] { target }, this);

        // 2. 所有敌人各自失去与自己当前层数相等的生命 (无视格挡; 没被点过的敌人层数为 0, 不掉血)。
        var enemies = CombatState?.HittableEnemies ?? [];
        foreach (var enemy in enemies)
        {
            var stacks = enemy.GetPower<Powers.DianXue>()?.Amount ?? 0;
            if (stacks <= 0) continue;
            await CreatureCmd.Damage(choiceContext, enemy, stacks, ValueProp.Unblockable,
                Owner.Creature, this, cardPlay);
        }
    }
}
