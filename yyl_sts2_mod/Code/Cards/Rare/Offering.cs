using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     投喂: 0 费, 给予所有奶龙 20 点格挡, 获得 4 → 6 炁, 获得 2 能量。
///     [balance 2026-09-19] 用户定调: 定位为"0 费产炁、带负面(给奶龙格挡)的高数值金卡"
///     —— 费用 2 → 0, 炁 2 → 4 (升级 6)。给敌人的 20 格挡是它的代价。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Offering(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Offering() : this(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<Qi>(4, 2);
        WithEnergy(2, 0);
        WithBlock(20, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        // 给所有奶龙 20 格挡 (这就是这张 0 费卡的代价)
        foreach (var c in combatState.Creatures)
        {
            if (!yylNailong.IsNailong(c)) continue;
            await CreatureCmd.GainBlock(c, DynamicVars.Block.IntValue, ValueProp.Move, cardPlay);
        }
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["Qi"].IntValue, this, cardPlay.Card);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
