using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     金光化炁（罕见能力 → 稀有）: 2 费（升级 1 费）。
///     获得 1→2 层[gold]金光化炁[/gold]：每消耗 1 层[gold]金光护体[/gold]，获得等量点[gold]炁[/gold]。
///     <para>
///         把"金光是消耗品"这件事变成资源循环 —— 与 掌心雷 / 白长虫 / 五雷正法
///         （三者都会消耗金光换每段 +1）直接联动，是金光流的稀有 payoff。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class JinGuangHuaQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public JinGuangHuaQi() : this(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<Powers.JinGuangHuaQi>(1, 1); // 1 → 2 层
        // WithCostUpgradeBy(-1);                 // 2 → 1 费
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Powers.JinGuangHuaQi>(
            choiceContext,
            new[] { Owner.Creature },
            DynamicVars["JinGuangHuaQi"].IntValue,
            Owner.Creature,
            cardPlay.Card);
    }
}
