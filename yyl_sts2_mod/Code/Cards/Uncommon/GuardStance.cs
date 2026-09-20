using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     守势: 1 费能力牌, 获得[守势] —— 金光护体每层额外减伤 1 → 2 点,
///     但炁不再为你的伤害提供加成。
///     <para>
///         攻防切换的宣言: 炁(攻) 与金光护体(防) 之间的单向阀门,
///         让"只堆金光、不堆炁"成为一条能走的路线。减伤与取消增伤分别由
///         <see cref="Powers.GoldenAegis" /> 与 <see cref="Powers.Qi" /> 读取。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class GuardStance(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public GuardStance() : this(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<ShouShi>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var amount = DynamicVars["ShouShi"].IntValue;
        await PowerCmd.Apply<ShouShi>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
    }
}
