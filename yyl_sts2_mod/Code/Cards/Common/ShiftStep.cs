using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
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
///     套步: 1 费, 抽 1 → 2 张, 然后选择 1 张手牌消耗 (原版「燃烧契约」式自选)。
///     廉价的滤牌: 抽得少, 但能精准烧掉废牌(尤其状态牌), 越到残局越有用。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ShiftStep(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ShiftStep() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithCards(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.Draw(this, choiceContext);

        // 抽完牌后从手牌选 1 张消耗 (选牌写法同拾遗; 空手牌时静默跳过)。
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null || hand.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, hand, Owner, prefs)).ToList();
        if (selected.Count == 0) return;
        await CardCmd.Exhaust(choiceContext, selected[0]);
    }
}
