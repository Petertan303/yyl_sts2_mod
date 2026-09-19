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

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     燃炁: 0 费, 失去 6 点生命, 获得 6 → 9 点炁, 抽 3 张。
///     对标原版"祭品": 用血量换一手资源。丹噬 / 天火 / 散炁 都能把这笔炁立刻放大。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class BurnQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public BurnQi() : this(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<Qi>(6, 3);
        WithCards(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 代价是直接的掉血: 不吃格挡、不受增减伤影响。
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 6m,
            ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature, this, cardPlay);
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["Qi"].IntValue, this, cardPlay.Card);
        await CommonActions.Draw(this, choiceContext);
    }
}
