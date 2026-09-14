using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     破防: 1 费, 造成 6 伤害 2 → 3 次, 每次命中使奶龙 -1 力量。消耗。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class GuardBreak(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public GuardBreak() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(6);
        WithVars(new RepeatVar(2).WithUpgrade(1));
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        var hits = DynamicVars.Repeat.IntValue;
        var attack = await CommonActions.CardAttack(this, cardPlay, target, DynamicVars.Damage.IntValue,
                ValueProp.Move, hitCount: hits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (!yylNailong.IsNailong(target)) return;
        var hitCount = attack.Results.SelectMany(result => result).Count(result => result.Receiver == target);
        for (var i = 0; i < hitCount; i++)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, new[] { target }, -1, Owner.Creature, cardPlay.Card);
        }
    }
}
