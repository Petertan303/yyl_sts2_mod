using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     引雷: 2 费, 造成 8 → 11 伤害, 并给予 4 → 6 层易伤。消耗。
///     <para>
///         「不纯粹」的易伤牌 (设计笔记 §4): 以高费 + 消耗为代价换取一次性的
///         大额伤害 + 铺易伤, 区别于常规直给型 debuff 牌。
///         先易伤后攻击的顺序让本次攻击直接吃到 1.5x。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class YinLei(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public YinLei() : this(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(8, 3);
        WithPower<VulnerablePower>(4, 2);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;

        // 先施加易伤, 让本次攻击直接吃到 1.5x (与掌心雷同序)。
        await CommonActions.Apply<VulnerablePower>(choiceContext, new[] { target }, this);
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_attack_lightning")
            .Execute(choiceContext);
    }
}
