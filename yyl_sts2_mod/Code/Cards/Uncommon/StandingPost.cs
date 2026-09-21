using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
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
///     站桩: 2 费, 获得 16 → 22 点格挡。
///     罕见位最朴素的防御裸卡: 无任何附加功能, 纯粹把费用换成格挡。
///     与移穴 (1 费 10 → 15 但要失去 2 炁) 分工: 站桩不需要炁, 移穴更便宜但有代价。
///     <para>
///         原为普通卡, 因 2 费 16 格挡已越过普通位防御上限而移入罕见。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class StandingPost(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public StandingPost() : this(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        // ★2026-09-21: 格挡拆成两半 —— 先拿一半, 另外一半要消耗 1 点炁才拿得到
        //   (总数与原来一致: 8+8=16 / 11+11=22)。
        WithBlock(8, 3);
        WithPower<Qi>("QiLoss", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        yylVfx.OnCreatureRaised(Owner.Creature, "vfx/vfx_block", 1f/3f); // 打出特效: 格挡火花起桩
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var half = DynamicVars.Block.IntValue;
        await CommonActions.CardBlock(this, cardPlay);
        // 后半段: 失去 1 点炁换取等量格挡; 炁不足则只有前半段 (耗炁卡统一判定)。
        var cost = DynamicVars["QiLoss"].IntValue;
        if (cost <= 0 || Owner.Creature.GetPower<Qi>()?.Amount < cost) return;
        await yylCmd.LoseQi(choiceContext, Owner, cost, this, cardPlay.Card);
        await CreatureCmd.GainBlock(Owner.Creature, half, ValueProp.Move, cardPlay, false);
    }
}
