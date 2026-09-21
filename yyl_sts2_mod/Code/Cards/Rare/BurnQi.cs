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
        // 自伤代价 (卡面用)
        WithCalculatedDamage("HpLoss", 6, (_, _) => 0m, 0, 0, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this);
        yylVfx.OnCreature(Owner.Creature, "vfx/vfx_fire_burst"); // 打出特效: 燃炁起火 // 打出动作: 施法帧动画
        // 代价是"失去生命"而非伤害: 不走伤害管线, 不吃格挡、不吃炁/姿态/遗物的增减伤。
        // (之前用 CreatureCmd.Damage 实现, 自伤会被炁增伤乘区放大 —— 已改 yylCmd.LoseHp。)
        yylCmd.LoseHp(Owner.Creature, DynamicVars["HpLoss"].IntValue);
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["Qi"].IntValue, this, cardPlay.Card);
        await CommonActions.Draw(this, choiceContext);
    }
}
