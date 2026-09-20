using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     温养: 1 费罕见能力牌, 获得 4 → 6 层[温养] —— 失去炁时获得等量格挡。
///     <para>
///         耗炁体系偏攻击与运转, 血量压力大; 温养把"失去炁"这个代价换成格挡,
///         让"攻转防"撑住血线。层数即格挡值, 与「丹噬」正好互补。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class Nurture(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Nurture() : this(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<Powers.Nurture>(
            Powers.Nurture.DefaultAmount,
            Powers.Nurture.UpgradeAmount);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var amount = DynamicVars[typeof(Powers.Nurture).Name].IntValue;
        await PowerCmd.Apply<Powers.Nurture>(
            choiceContext,
            new[] { Owner.Creature },
            amount,
            Owner.Creature,
            cardPlay.Card);
    }
}
