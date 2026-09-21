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
///     金光护体源: 2 → 1 费能力牌, 每回合开始时获得 1 层金光护体。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class GoldenWardCard(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public GoldenWardCard() : this(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<GoldenWard>(1);
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this);
        yylVfx.OnCreature(Owner.Creature, "vfx/vfx_block"); // 打出特效: 金光护体格挡火花 // 打出动作: 施法帧动画
        var amount = DynamicVars["GoldenWard"].IntValue;
        await PowerCmd.Apply<GoldenWard>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
    }
}
