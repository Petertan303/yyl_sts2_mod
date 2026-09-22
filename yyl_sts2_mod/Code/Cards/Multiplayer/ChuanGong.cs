using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Multiplayer;

/// <summary>
///     传功: 1->0 费联机卡 (罕见)。失去 3 点炁，所有队友各获得 3 点炁。
///     <para>
///         伊林把签名资源炁渡给全队 —— 原版"给队友能量"的伊林版。联机专属
///         (TargetType.AllAllies)，单人局不会出现。无队友时不生效也不失炁。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ChuanGong(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ChuanGong() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 队友 = 其他存活的玩家 (排除自身)。单人局/无盟友时列表为空。
        var allies = Owner.Creature.CombatState
            .GetTeammatesOf(Owner.Creature)
            .Where(c => c != Owner.Creature && c.IsAlive && c.Player != null)
            .ToList();
        if (allies.Count == 0) return;

        // 炁不足 3 时无额外效果 (耗炁卡统一判定)。
        if (Owner.Creature.GetPower<Qi>()?.Amount >= 3)
        {
            await yylCmd.LoseQi(choiceContext, Owner, 3, this, cardPlay.Card);
            foreach (var ally in allies)
                await yylCmd.GainQi(choiceContext, ally.Player!, 3, this, cardPlay.Card);
        }
    }
}
