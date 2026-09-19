using BaseLib.Abstracts;
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

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     投喂: 0 费, 为所有奶龙回复 20 点生命; 每为一只奶龙**实际**回复了生命,
///     获得 4 → 6 点炁。
///     <para>
///         [rework 2026-09-19] 用户定调: 由"给奶龙 20 格挡 + 固定产炁 + 2 能量"
///         改为"回血触发产炁"—— 奶龙(多为敌人)满血时一分炁都拿不到,
///         所以本质是"敌人越残、数量越多, 这张牌越赚"的 0 费引擎;
///         也天然成了联机支援卡(给队友回血照产炁)。
///         注意与黑色幽默区分: 黑色幽默只回非队友奶龙且是固定产炁。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class Offering(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public Offering() : this(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithHeal(20, 0);
        // 每实际回复一只奶龙获得的炁 (升级 4 → 6)
        WithPower<Qi>("QiPer", 4, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 为所有奶龙回血 (含队友 —— 联机时这是支援面); 满血的奶龙回不上血, 不计入。
        var healed = 0;
        foreach (var c in combatState.Creatures)
        {
            if (!yylNailong.IsNailong(c)) continue;
            var before = c.CurrentHp;
            await CreatureCmd.Heal(c, DynamicVars.Heal.IntValue);
            if (c.CurrentHp > before) healed++;
        }

        // 每实际回复一只奶龙, 获得 {QiPer} 点炁。
        if (healed > 0)
            await yylCmd.GainQi(choiceContext, Owner, healed * DynamicVars["QiPer"].IntValue, this, cardPlay.Card);
    }
}
