using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     太极: 1 费, 失去 2 点炁, 本回合你受到的伤害转移给一名随机敌人。
///     <para>
///         机制化 AoE / 防御 (设计笔记 §6-②): 借力打力, 以彼之道还施彼身。
///         转移由 <see cref="Powers.TaiChiMark" /> + Harmony 前缀
///         (<see cref="Patches.TaiChiRedirectPatch" />) 实现: 打出期间所有以你为
///         目标的单体重定向到随机敌人; 回合结束 (敌方回合收尾) 自动散去。
///         [rule 2026-09-18] 耗炁卡统一判定: 炁不足 2 时无额外效果。
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
    public TaiChi() : this(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<TaiChiMark>(1);
        // 仅用于卡面显示: 这张卡要花掉的炁
        WithPower<Qi>("QiLoss", 2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 炁不足 2 时无额外效果 (耗炁卡统一判定)。
        if (Owner.Creature.GetPower<Qi>()?.Amount >= 2)
        {
            await yylCmd.LoseQi(choiceContext, Owner, 2, this, cardPlay.Card);
            var amount = DynamicVars["TaiChiMark"].IntValue;
            if (amount > 0)
                await PowerCmd.Apply<TaiChiMark>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
        }
    }
}
