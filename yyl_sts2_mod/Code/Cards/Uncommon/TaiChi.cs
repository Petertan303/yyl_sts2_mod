using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     太极: 0 费, 失去 2 点炁 (升级后 1 点), 本回合你受到的伤害转移给一名随机敌人。
///     <para>
///         机制化 AoE / 防御 (设计笔记 §6-②): 借力打力, 以彼之道还施彼身。
///         转移由 <see cref="Powers.TaiChiMark" /> + Harmony 前缀
///         (<see cref="Patches.TaiChiRedirectPatch" />) 实现: 打出期间所有以你为
///         目标的单体重定向到随机敌人; 回合结束 (敌方回合收尾) 自动散去。
///         [rule 2026-09-18] 耗炁卡统一判定: 炁不足时无额外效果。
///         [balance 2026-09-19] 稀有度 Rare → Uncommon; 升级效果改为耗炁 2 → 1 (用户定调)。
///         [balance 2026-09-21] 费用 1 → 0 (用户定调); 耗炁维持 3 → 2。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class TaiChi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public TaiChi() : this(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<TaiChiMark>(1);
        // 仅用于卡面显示 + 实际读取: 这张卡要花掉的炁 (2026-09-21 上调: 升级 3 → 2)
        WithPower<Qi>("QiLoss", 3, -1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this);
        yylVfx.OnCreature(Owner.Creature, "vfx/common/vfx_common_ring_polar_a"); // 打出特效: 阴阳环 // 打出动作: 施法帧动画
        // 炁不足时无额外效果 (耗炁卡统一判定); 消耗量随升级变化 (3 → 2)。
        var cost = DynamicVars["QiLoss"].IntValue;
        if (cost > 0 && Owner.Creature.GetPower<Qi>()?.Amount >= cost)
        {
            await yylCmd.LoseQi(choiceContext, Owner, cost, this, cardPlay.Card);
            var amount = DynamicVars["TaiChiMark"].IntValue;
            if (amount > 0)
                await PowerCmd.Apply<TaiChiMark>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
        }
    }
}
