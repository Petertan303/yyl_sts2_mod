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
///     丹噬: 2 费罕见(蓝)能力牌, 获得 2 → 3 层[丹噬] —— 获得炁时对所有敌人造成「本次获得炁量 × 层数」点伤害。
///     <para>
///         产炁体系本是防御向, 缺输出; 丹噬把"获得炁"这个动作直接换成群伤,
///         让"防转攻"成为一条独立路线。层数即伤害值, 与「温养」正好互补。
///         [balance 2026-09-22] 稀有度 Rare → Uncommon(蓝卡), 层数 1→2 提升为 2→3 (用户定调)。
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
    public CinnabarBite() : this(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
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
