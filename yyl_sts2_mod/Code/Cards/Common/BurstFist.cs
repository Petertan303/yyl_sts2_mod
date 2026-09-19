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

/// <summary>
///     崩拳: 2 费, 造成 14 → 18 点伤害。
///     普通位唯一的高数值单段攻击 (对应原版重锤/埋葬那一档), 前期过渡的输出担当。
///     与阿威十八式 (2 费 3 段) 分工: 单段吃减伤更划算, 多段吃狂热/多段加成。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class BurstFist(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public BurstFist() : this(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(14, 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
