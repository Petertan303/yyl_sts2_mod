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
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     崩拳: 2 费, 造成 14 → 18 点伤害; 若你本回合获得过[炁], 获得 2 点能量。
///     <para>
///         [rework 2026-09-19] 用户定调: 作为普通位的"伪 0 费"牌 —— 代价是
///         本回合必须先产出过炁 (产炁本身占卡/占费), 判定走 <see cref="QiGainTracker" />
///         (yylCmd.GainQi 是全 mod 唯一产炁入口, 含遗物/Power 途径)。
///         注意: 开局黄桃罐头自带的 3 炁也计入, 所以第一回合它天然是净 0 费。
///     </para>
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
        WithEnergy(2, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        await CommonActions.CardAttack(this, cardPlay)
            .Execute(choiceContext);
        // 华丽收场的命中冲击 (替代原 slash 命中特效)。
        yylVfx.GrandFinaleImpact(target);

        // 伪 0 费: 本回合获得过炁 → 返还 2 能量。
        if (QiGainTracker.GainedThisRound(Owner, Owner.Creature.CombatState))
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}
