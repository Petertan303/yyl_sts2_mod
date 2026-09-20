using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     丹噬: 2 费稀有能力牌, 获得 4 → 6 层[丹噬] —— 获得炁时对所有敌人造成等量伤害。
///     <para>
///         产炁体系本是防御向, 缺输出; 丹噬把"获得炁"这个动作直接换成群伤,
///         让"防转攻"成为一条独立路线。层数即伤害值, 与「温养」正好互补。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class CinnabarBite(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public CinnabarBite() : this(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<Powers.CinnabarBite>(
            Powers.CinnabarBite.DefaultAmount,
            Powers.CinnabarBite.UpgradeAmount);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var amount = DynamicVars[typeof(Powers.CinnabarBite).Name].IntValue;
        await PowerCmd.Apply<Powers.CinnabarBite>(
            choiceContext,
            new[] { Owner.Creature },
            amount,
            Owner.Creature,
            cardPlay.Card);
    }
}
