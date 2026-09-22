using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     散炁: 0 费白卡, 失去 2 炁, 获得 2 → 3 能量。能量引擎 (抓复数当运转件)。
///     [rule 2026-09-18] 耗炁卡统一判定: 炁不足 2 时无额外效果 (不获得能量)。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class QiDispersal(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public QiDispersal() : this(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithEnergy(2, 1);
        // 仅用于卡面显示: 这张卡要花掉的炁
        WithPower<Qi>("QiLoss", 2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 炁不足 2 时无额外效果 (耗炁卡统一判定)。
        if (Owner.Creature.GetPower<Qi>()?.Amount >= 2)
        {
            await yylCmd.LoseQi(choiceContext, Owner, 2, this, cardPlay.Card);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
    }
}
