using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     阿威十八式: 2 费, 造成 2 → 3 点伤害 5 次。
///     「十八式」取招式繁多之意, 用多段体现; 与破防(降力量)定位错开, 本卡是纯粹输出。
///     <para>
///         [balance 2026-10-02] **由蓝卡降级为白卡** (目录 Uncommon → Common), 每段伤害 3→2、
///         费用不变仍为 2。降级理由与「引雷」正好互补: 引雷(蓝)走**易伤**铺场,
///         本卡走**纯段数输出**, 两者不重叠; 而本卡与同为蓝卡的「狂热」(每层对奶龙额外 +2 伤)
///         天然配合 —— 降到白卡后, 前期抓到这两张任意一张时, 去抓另一张的动力都更强
///         (一白一蓝, 都在早期卡池里)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class EighteenForms(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public EighteenForms() : this(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(2, 1);
        WithVars(new RepeatVar(6));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_dagger_spray_flurry")
            .Execute(choiceContext);
    }
}
