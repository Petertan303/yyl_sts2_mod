using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     阴煞: 2 → 1 费, 给予所有敌人 99 层虚弱 (≈ 贯穿整场战斗)。不消耗。
///     <para>
///         「不消耗但高费」的虚弱牌 (设计笔记 §4), 模仿猎手「恐怖」的全场虚弱思路。
///         注意: 原计划书锚点为"0 费", 但 0 费 + 不消耗 + 99 层会同时加重
///         免费浓度与常驻强度, 落地时按设计原则改为 2 费 (升级降费)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class YinSha(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public YinSha() : this(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(99);
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = CombatState?.HittableEnemies ?? [];
        var weak = DynamicVars["WeakPower"].IntValue;
        if (weak > 0)
            await PowerCmd.Apply<WeakPower>(choiceContext, enemies, weak, Owner.Creature, cardPlay.Card);
    }
}
