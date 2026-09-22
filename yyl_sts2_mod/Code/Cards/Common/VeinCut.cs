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

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     截脉: 1 费白卡, 造成 3 点伤害, 给予 2 → 3 层虚弱, 消耗。
///     与引雷 (易伤 + 消耗) 对称的另一半; 压成纯虚弱赋能件, 伤害仅作添头。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class VeinCut(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public VeinCut() : this(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(3, 0);
        WithPower<WeakPower>(2, 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_bloody_impact")
            .Execute(choiceContext);
        await CommonActions.Apply<WeakPower>(choiceContext, new[] { target }, this);
    }
}
