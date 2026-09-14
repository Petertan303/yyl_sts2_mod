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
///     投喂: 1 费, 给予所有奶龙 10 格挡, 获得 2 → 3 炁, 获得 2 能量。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class TouWei(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public TouWei() : this(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<Qi>(2, 1);
        WithEnergy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        // 给所有奶龙 10 格挡
        foreach (var c in combatState.Creatures)
        {
            if (!yylNailong.IsNailong(c)) continue;
            await CreatureCmd.GainBlock(c, 10m, ValueProp.Move, cardPlay);
        }
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["Qi"].IntValue, this, cardPlay.Card);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
