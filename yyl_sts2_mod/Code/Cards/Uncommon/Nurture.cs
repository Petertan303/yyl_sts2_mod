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
///     温养: 1 费(升级不降费)罕见能力牌, 获得 2 → 3 层[温养] —— 获得炁时获得 (获得炁量 × 温养层数) 格挡。
///     <para>
///         ★2026-11-17 用户定调: 取消"升级降为 0 费"(玩家反馈 0 费开难度太低), 费用固定 1;
///         升级效果改为层数 2 → 3 (原为固定 3 层)。数值以常规 1–3 点炁流量
///         (蓄势 / 炁海 / 食炁返还 / 马步) 为基准, 不以战斗开始遗物的 3 炁或天师度的 30 炁为基准。
///         与「丹噬」(失去炁→中毒) 互补: 丹噬把耗炁换算成延迟输出, 温养把产炁换算成防御。
///         温养层数即倍率, 攒一大笔炁再吃下去才是正确用法。
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
        // ★2026-11-17: 费用固定 1(去掉 WithCostUpgradeBy(-1), 不再 0 费开);
        //   获得 2 层温养, 升级 +1 → 3 层 (层数即倍率)。
        WithPower<Powers.Nurture>(2, 1);
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
