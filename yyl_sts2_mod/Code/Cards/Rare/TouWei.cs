using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     投喂: 1 费, 给予所有奶龙 10 格挡, 获得 2 → 3 炁, 获得 2 能量。
///     <para>
///         TODO: <c>CreatureCmd.GainBlock</c> 方法名待 BaseLib 实际 API 确认。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class TouWei(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public TouWei() : this(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        var qi = 2; // TODO upgrade: would be 3 if IsUpgraded
        // 给所有奶龙 10 格挡
        foreach (var c in combatState.Creatures)
        {
            if (!c.HasPower<NlPower>() && !c.HasPower<NlPowerPlus>()) continue;
            // TODO: replace with actual CreatureCmd.GainBlock / AddBlock when API confirmed
            // await CreatureCmd.GainBlock(c, 10);
        }
        await yylCmd.GainQi(choiceContext, Owner, qi, this, cardPlay.Card);
        await PlayerCmd.GainEnergy(2, Owner);
    }
}
