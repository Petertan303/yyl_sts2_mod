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
///     拔罐: 1 费能力牌, 获得[拔罐] 1 → 2 层 —— 每回合开始时随机移除自身等量层数的负面状态。
///     慢工型净化: 回合越多清得越多, 与涤荡 (一次性全清) 分工。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Cupping(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Cupping() : this(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<BaGuan>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var amount = DynamicVars["BaGuan"].IntValue;
        await PowerCmd.Apply<BaGuan>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
    }
}
