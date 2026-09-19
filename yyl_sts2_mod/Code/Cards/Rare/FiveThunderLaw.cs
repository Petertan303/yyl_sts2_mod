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

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     五雷正法: 3 费, 对所有敌人造成 18 → 24 点伤害。
///     唯一一张"不需要炁"的大 AoE: 天火要靠囤炁, 这张靠纯费用, 两条清场路线互不打架。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class FiveThunderLaw(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public FiveThunderLaw() : this(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(18, 6);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
