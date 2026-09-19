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
///     破绽: 1 费, 给予 1 → 2 层易伤与 1 层虚弱。
///     易伤与虚弱一起给, 但都很薄 —— 升级只加易伤, 虚弱层数固定不涨 (浓度收口)。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Opening(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Opening() : this(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithPower<VulnerablePower>(1, 1);
        WithPower<WeakPower>(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        await CommonActions.Apply<VulnerablePower>(choiceContext, new[] { target }, this);
        await CommonActions.Apply<WeakPower>(choiceContext, new[] { target }, this);
    }
}
