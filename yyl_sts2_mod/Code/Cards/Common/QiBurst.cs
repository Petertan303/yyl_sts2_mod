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
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     炁冲: 1 费白卡, 造成 6 → 9 点伤害; 若拥有炁, 消耗 1 点炁额外造成一次 (共两段)。
///     把炁直接换成单体伤害: 基础一段, 有炁时再补一段——鼓励攒炁但不强制。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class QiBurst(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public QiBurst() : this(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        WithDamage(6, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        // 怪物特效挪用: 同族祭司灵魂光束, 从角色中线镜像后朝右射向目标。
        yylVfx.KinBeam(Owner.Creature, flipX: true);
        // 基础一段
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_attack_lightning")
            .Execute(choiceContext);
        // 有炁则消耗 1 点炁, 额外造成一次 (2026-09-22 重做)
        if (Owner.Creature.GetPower<Qi>()?.Amount >= 1)
        {
            await yylCmd.LoseQi(choiceContext, Owner, 1, this, cardPlay.Card);
            await CommonActions.CardAttack(this, cardPlay)
                .WithHitFx("vfx/vfx_attack_lightning")
                .Execute(choiceContext);
        }
    }
}
