using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Basic;

[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public sealed class WardingBlade(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public WardingBlade() : this(1, CardType.Attack, CardRarity.Basic, TargetType.AllEnemies)
    {
        WithDamage(1, 1);
        WithVars(new RepeatVar(5));
        WithPower<VulnerablePower>(1);
        WithPower<WeakPower>(1);
        
        WithPower<GoldenAegis>(-1);
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = CombatState?.HittableEnemies ?? [];
        await CommonActions.Apply<VulnerablePower>(choiceContext, enemies, this);
        await CommonActions.Apply<WeakPower>(choiceContext, enemies, this);

        if (Owner.HasPower<GoldenAegis>()){
            DynamicVars.Damage.BaseValue += 1;
            await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
        }

        await CommonActions.CardAttack(this, cardPlay)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .WithValueProp(ValueProp.Unblockable)
            .Execute(choiceContext);
    }
}
