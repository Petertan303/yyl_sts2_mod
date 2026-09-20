using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     回收: 1 → 0 费, 消耗手牌中的 1 张牌, 获得等同其费用的能量。
///     <para>
///         [new 2026-09-20] 用户要求从一代机器人的「回收」直接移植;
///         与拾遗 (回收弃牌堆, 置 0 费) 分工: 回收把废牌直接换成能量当回合兑现。
///         选牌 UI 用 FromHand + ExhaustSelectionPrompt (原版燃烧契约同款)。
///         费用读取走 card.EnergyCost.GetResolved() (含临时费用修正与 X 费捕获值)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Recycle(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Recycle() : this(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null || hand.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
        var selected = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this)).ToList();
        if (selected.Count == 0) return;

        var card = selected[0];
        var cost = card.EnergyCost.GetResolved();
        if (cost < 0) cost = 0;

        await CardCmd.Exhaust(choiceContext, card);
        await PlayerCmd.GainEnergy(cost, Owner);
    }
}
