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
///     站桩: 2 费, 获得 16 → 22 点格挡 —— 拆成两段, 第二段需要消耗 2 层金光护体。
///     ★2026-11-02: 代价由 1 炁改为 2 金光 (用户定调, 与金光化炁联动)。
///     ★2026-11-02 (雪藏, 用户定调): 效果整体上移到「返照」(BaoShan, 消耗全部金光的严格上位版:
///     同款两段格挡 + 2 倍回流引擎), 本卡改为 <see cref="CardRarity.Basic" /> 雪藏不出。
///     代码保留 —— 解雪藏只需把稀有度改回 Uncommon。
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
    public StandingPost() : this(2, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
        // ★2026-09-21: 格挡拆成两半 —— 先拿一半, 另外一半要消耗 2 层金光才拿得到
        //   (总数与原来一致: 8+8=16 / 11+11=22)。
        // ★2026-11-02: 代价 1 炁 → 2 金光 (用户定调, 与金光化炁联动)。
        // ★2026-11-02: 稀有度 Uncommon → Basic (雪藏) —— 效果上移到「返照」, 本卡不出。
        WithBlock(8, 3);
        WithPower<GoldenAegis>("AegisCost", 2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        yylVfx.OnCreatureRaised(Owner.Creature, "vfx/vfx_block", 1f/3f); // 打出特效: 格挡火花起桩
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var half = DynamicVars.Block.IntValue;
        await CommonActions.CardBlock(this, cardPlay);
        // 后半段: 消耗 2 层金光换取等量格挡; 金光不足则只有前半段。
        //   走统一入口 yylAegis.Consume —— 「金光化炁」在此挂钩 (2 层金光 → 2 炁)。
        var cost = DynamicVars["AegisCost"].IntValue;
        if (cost <= 0 || Owner.Creature.GetPower<GoldenAegis>()?.Amount < cost) return;
        await yylAegis.Consume(choiceContext, this, cost);
        await CreatureCmd.GainBlock(Owner.Creature, half, ValueProp.Move, cardPlay, false);
    }
}
