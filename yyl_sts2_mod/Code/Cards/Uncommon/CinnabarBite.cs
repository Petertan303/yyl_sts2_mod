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
///     丹噬: 1 费罕见(蓝)能力牌, 获得 2 → 3 层[丹噬] —— 失去炁时对所有敌人施加「实际失炁量 × 层数」层中毒。
///     <para>
///         耗炁牌偏进攻/资源倾泻; 丹噬把"失去炁"这个动作换成全体叠毒 (延迟输出),
///         与「温养」(获得炁→格挡) 构成攻防对调。层数即中毒倍率。
///         [balance 2026-09-22] 稀有度 Rare → Uncommon(蓝卡)。
///         [2026-11-08 用户定调] 直伤 → 施加中毒; 触发端获得炁 → 失去炁; 层数 1 → 2 改为 2 → 3。
///         数值以常规 1–3 点炁流量 (炁化金光 / 炁冲 / 移穴 / 食炁) 为基准, 不以天师度/天火/神游为基准。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class CinnabarBite(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public CinnabarBite() : this(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
        WithPower<Powers.CinnabarBite>(
            Powers.CinnabarBite.DefaultAmount,
            Powers.CinnabarBite.UpgradeAmount);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this);
        yylVfx.OnCreature(Owner.Creature, "vfx/vfx_poison_impact"); // 打出特效: 毒 (丹噬) // 打出动作: 施法帧动画
        var amount = DynamicVars[typeof(Powers.CinnabarBite).Name].IntValue;
        await PowerCmd.Apply<Powers.CinnabarBite>(
            choiceContext,
            new[] { Owner.Creature },
            amount,
            Owner.Creature,
            cardPlay.Card);
    }
}
