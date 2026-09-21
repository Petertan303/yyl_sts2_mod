using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     风后奇门: 1 费, 给予所有敌人 1 → 2 层虚弱, 抽 1 张牌。
///     八奇技之一, 主"布局操控"; 以削弱对手并争取先机来体现。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class WindArray(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public WindArray() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(1, 1);
        WithCards(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 原版华丽收场的花瓣粒子: 从玩家位置向右洒落, 扫过敌阵 ("风"起)。
        yylVfx.BurstOneShot(Owner.Creature, "vfx/grand_finale/vfx_grand_finale_petals");
        var enemies = CombatState?.HittableEnemies ?? [];
        await CommonActions.Apply<WeakPower>(choiceContext, enemies, this);
        await CommonActions.Draw(this, choiceContext);
    }
}
