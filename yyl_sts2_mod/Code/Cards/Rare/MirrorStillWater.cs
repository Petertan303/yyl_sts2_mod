using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     明镜止水: X 费(金/稀有)技能牌, 消耗。
///     打出时花光所有剩余能量 (X); 下回合开始时获得 X 点能量, 并抽 X 张牌。
///     升级后改为 X+1 (等量能量 + 等量抽牌, 各 +1)。
///     <para>
///         复用原版两个 next-turn power:
///         - <see cref="EnergyNextTurnPower"/> —— 下回合获得等量能量;
///         - <see cref="DrawCardsNextTurnPower"/> —— 下回合抽等量牌。
///         X 值取自 card.EnergyCost.CapturedXValue (引擎花光能量时写入, 不会自动乘, 须自行缩放)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class MirrorStillWater(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public MirrorStillWater() : this(int.MinValue, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        // X 费机制: 引擎以虚拟属性 HasEnergyCostX 决定 CostsX (见 CardModel 源码
        // `_energyCost = new CardEnergyCost(this, CanonicalEnergyCost, HasEnergyCostX)`)。
        // 仅靠 canonical = Int32.MinValue 并不会点亮 CostsX —— 必须显式覆写 HasEnergyCostX => true。
        // 点亮后: 打出时引擎花光全部剩余能量, 并写入 EnergyCost.CapturedXValue (= 本次花费的 X)。
        // 效果在 OnPlay 里读 CapturedXValue 缩放 (引擎不会自动乘)。
        WithKeywords(CardKeyword.Exhaust);
    }

    /// <summary>
    ///     X 费开关。覆写为 true 后, 引擎把此卡视为 X 费:
    ///     打出时花光所有剩余能量, 并将花费值写入 EnergyCost.CapturedXValue。
    /// </summary>
    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 本回合花费的能量 (X); 升级额外 +1。
        var spent = cardPlay.Card.EnergyCost.CapturedXValue;
        decimal amount = (decimal)spent + (IsUpgraded ? 1m : 0m);
        if (amount <= 0) return;

        // 下回合获得等量能量
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
        // 下回合抽等量牌
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
    }
}
