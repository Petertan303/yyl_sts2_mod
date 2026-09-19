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
///     化劲: 1 费, 获得 5 → 8 点格挡, 并获得 1 层[卸力] (下一次受到的攻击伤害减半)。
///     卸力由 <see cref="Powers.XieLi" /> 实现: 伤害预览不会消耗它, 真挨打后才失效。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class DeflectForce(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public DeflectForce() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(5, 3);
        WithPower<XieLi>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        var xieli = DynamicVars["XieLi"].IntValue;
        await PowerCmd.Apply<XieLi>(choiceContext, new[] { Owner.Creature }, xieli, Owner.Creature, cardPlay.Card);
    }
}
