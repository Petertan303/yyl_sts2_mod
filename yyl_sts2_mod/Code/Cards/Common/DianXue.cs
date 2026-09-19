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
///     点穴: 1 费, 给予所有敌人 1 → 2 层[点穴], 然后每个敌人失去等同自身点穴层数的生命
///     (无视格挡), 再抽 1 张牌。
///     <para>
///         层数会留在目标身上累积, 所以反复点同一个敌人会越来越痛 ——
///         普通攻击位里少见的长线钩子, 配合多段/连击有额外收益。
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
    public DianXue() : this(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        WithPower<Powers.DianXue>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 照搬观者 mod 的「点穴」: 先给所有敌人上点穴, 再让它们失去等同自身层数的生命。
        var enemies = CombatState?.HittableEnemies ?? [];
        await CommonActions.Apply<Powers.DianXue>(choiceContext, enemies, this);
        foreach (var enemy in enemies)
        {
            var stacks = enemy.GetPower<Powers.DianXue>()?.Amount ?? 0;
            if (stacks <= 0) continue;
            await CreatureCmd.Damage(choiceContext, enemy, stacks, ValueProp.Unblockable,
                Owner.Creature, this, cardPlay);
        }
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }
}
