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

namespace yyl_sts2_mod.Code.Cards.Common;
using MegaCrit.Sts2.Core.Models;

/// <summary>
///     舒筋: 1 费, 移除自身 2 → 3 层负面状态, 并把它们原样转移到目标敌人身上。消耗。
///     <para>
///         "净化"里唯一一张能把负面丢回去的牌; 代价是消耗 (一次性)。
///         与涤荡(全清但只清自己)、拔罐(每回合慢慢清) 分工明确。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class RelaxTendon(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public RelaxTendon() : this(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithCards(2, 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        var layers = DynamicVars.Cards.IntValue;
        for (var i = 0; i < layers; i++)
        {
            var debuff = Owner.Creature.Powers
                .FirstOrDefault(p => p.Type == PowerType.Debuff && p.Amount > 0);
            if (debuff == null) break;

            // 用 AllPowers 里的规范实例把"同一种负面"施加给敌人, 再扣掉自己身上的 1 层。
            var canonical = ModelDb.AllPowers.FirstOrDefault(p => p.GetType() == debuff.GetType());
            if (canonical == null) break;
            await PowerCmd.Apply(choiceContext, canonical, target, 1m, Owner.Creature, cardPlay.Card);
            await PowerCmd.ModifyAmount(choiceContext, debuff, -1m, Owner.Creature, cardPlay.Card);
        }
    }
}
