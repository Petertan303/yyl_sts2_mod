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
///     开脉: 2 → 1 费能力牌。战斗结束后, 选择 1 张牌组中的牌永久附魔「炁脉」
///     (打出时额外获得 1 点炁)。
///     <para>
///         局外资源入口: 附魔写在牌组上、跨战斗保留, 是原版角色没有的资源类型。
///         数值上刻意压低 (1 费 1 张), 收益全部体现在后续战斗里。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class MeridianOpening(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public MeridianOpening() : this(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCostUpgradeBy(-1);
        WithPower<Powers.MeridianOpening>(1);
        // 仅用于卡面显示: 附魔几张牌
        WithCards(1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var amount = DynamicVars[typeof(Powers.MeridianOpening).Name].IntValue;
        await PowerCmd.Apply<Powers.MeridianOpening>(
            choiceContext,
            new[] { Owner.Creature },
            amount,
            Owner.Creature,
            cardPlay.Card);
    }
}
