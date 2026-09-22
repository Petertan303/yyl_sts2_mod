using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     涤荡: 2 → 1 费, 移除你所有的负面状态 (清除层数, 而非当回合免疫)。
///     <para>
///         「永久清除」型净化 (设计笔记 §5 罕见位): 与「清心咒」的
///         "当回合无效"区分语义。直接用 <see cref="PowerCmd.Remove(PowerModel)" />
///         逐个移除 Debuff 类型的 Power, 保留 Buff 不动。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Purge(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Purge() : this(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        yylVfx.OnCreatureRaised(Owner.Creature, "vfx/vfx_smoke_puff", 1f/3f); // 打出特效: 浊气散去
        var debuffs = Owner.Creature.Powers
            .Where(p => p.Type == PowerType.Debuff)
            .ToList();
        foreach (var debuff in debuffs)
            await PowerCmd.Remove(debuff);
    }
}
