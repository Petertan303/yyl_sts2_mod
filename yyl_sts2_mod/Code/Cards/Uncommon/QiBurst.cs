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

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     炁冲: 1 费, 造成 12 → 18 点伤害, 失去 1 点炁 (炁不足则不失去, 伤害照常)。
///     把炁直接换成单次伤害: 天火是"一次清空换群伤", 这张是"一点一点换单体"。
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
    public QiBurst() : this(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        WithDamage(12, 6);
        // 仅用于卡面显示: 这张卡要花掉的炁
        WithPower<Qi>("QiLoss", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;
        if (Owner.Creature.GetPower<Qi>()?.Amount >= 1)
            await yylCmd.LoseQi(choiceContext, Owner, 1, this, cardPlay.Card);
        // 怪物特效挪用: 同族祭司灵魂光束, 从角色中线镜像后朝右射向目标。
        yylVfx.KinBeam(Owner.Creature, flipX: true);
        await CommonActions.CardAttack(this, cardPlay)
            .WithHitFx("vfx/vfx_attack_lightning")
            .Execute(choiceContext);
    }
}
