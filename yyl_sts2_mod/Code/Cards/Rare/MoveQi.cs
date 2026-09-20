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

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     行炁: 2 → 1 费能力牌, 获得[行炁] —— 每当你失去炁, 抽 1 张牌。
///     <para>
///         耗炁流的过牌核心: 温养把"失去炁"换成格挡, 行炁换成过牌。
///         配合通畅 / 散炁 / 炁化金光, 可以把同一份炁同时当伤害、能量和过牌用。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class MoveQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public MoveQi() : this(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<XingQi>(1);
        // 仅用于卡面显示: 每次失去炁时抽几张
        WithPower<XingQi>("DrawCount", 1, 0);
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        await PowerCmd.Apply<XingQi>(choiceContext, new[] { Owner.Creature }, 1, Owner.Creature, cardPlay.Card);
    }
}
