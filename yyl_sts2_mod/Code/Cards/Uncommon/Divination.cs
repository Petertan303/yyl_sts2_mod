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

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     卜卦: 1 费, 预视 3 → 4 张 (查看抽牌堆顶并任意弃掉), 然后抽 2 张。
///     本角色少数几张"专职过牌/牌库控制"的牌之一 —— 过牌集中在少数卡上, 而不是人人都能抽。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Divination(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Divination() : this(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithCards(3, 1);
        // 预视之后的抽牌数 (卡面用)
        WithCalculatedDamage("Draw", 2, (_, _) => 0m, 0, 0, 0);
        // ★2026-09-21: 改为 0 费, 抽牌以"失去 1 点炁"为前提 —— 没有炁就只预视。
        WithPower<Qi>("QiLoss", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this);
        yylVfx.OnCreature(Owner.Creature, "vfx/vfx_starry_impact"); // 打出特效: 星象 (原版场景) // 打出动作: 施法帧动画
        // 预视始终生效 (0 费的部分)。
        await ScryCmd.Execute(choiceContext, this);
        // ★抽牌以"失去 1 点炁"为前提: 炁不足时只预视、不抽 (耗炁卡统一判定)。
        var cost = DynamicVars["QiLoss"].IntValue;
        if (cost <= 0 || Owner.Creature.GetPower<Qi>()?.Amount < cost) return;
        await yylCmd.LoseQi(choiceContext, Owner, cost, this, cardPlay.Card);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].IntValue, Owner);
    }
}
