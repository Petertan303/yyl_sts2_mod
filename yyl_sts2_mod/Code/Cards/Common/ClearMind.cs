using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     清心咒: 1 费, 获得 4 → 7 格挡, 本回合你受到的负面状态无效 (免疫新挂的
///     debuff, 不清除已有层数)。
///     <para>
///         「当回合无效」型净化 (设计笔记 §5 普通位): 与「涤荡」的"永久清除"
///         区分语义。免疫由 <see cref="Powers.PurityVeil" /> 实现 —— 仿原版
///         圣物 (Artifact) 的 <c>TryModifyPowerAmountReceived</c> 把负面状态
///         的获得量改写为 0, 回合结束自动散去。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ClearMind(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ClearMind() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(4, 3);
        WithPower<PurityVeil>(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        var amount = DynamicVars["PurityVeil"].IntValue;
        if (amount > 0)
            await PowerCmd.Apply<PurityVeil>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
    }
}
