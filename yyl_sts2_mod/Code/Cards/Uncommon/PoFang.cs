using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Compatibility;
using yyl_sts2_mod.Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     破防: 1 费, 造成 6 伤害 2 → 3 次, 每次命中使奶龙 -1 力量。消耗。
///     <para>
///         TODO: 升级档位检测 (IsUpgraded / UpgradeType) 需用 BaseLib 的实际属性名。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class PoFang(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public PoFang() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        // TODO: 用 IsUpgraded 替换;目前固定 2 hits
        var hits = 2;
        for (var i = 0; i < hits; i++)
        {
            await CompatibilityCreatureCmd.Damage(
                choiceContext, target, 6, default(ValueProp), cardPlay.Card, cardPlay);
            if (IsNailong(target))
            {
                await PowerCmd.Apply<StrengthPower>(choiceContext, new[] { target }, -1, Owner.Creature, cardPlay.Card);
            }
        }
    }

    private static bool IsNailong(MegaCrit.Sts2.Core.Entities.Creatures.Creature c) =>
        c.HasPower<NlPower>() || c.HasPower<NlPowerPlus>();
}
