using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Utils;
using MegaCrit.Sts2.Core.Models.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     黑色幽默: 2 费, 为奶龙回复 20 HP, 获得 3 炁, 获得 1 层无实体。基础消耗, 升级后去除消耗。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class DarkHumor(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public DarkHumor() : this(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<IntangiblePower>(1, 0);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        // 为非队友的奶龙回复 20 HP (自己也可能算在内 —— 大瓶黄桃罐头会把玩家也视为奶龙)。
        // 队友不回血: 联机时给队友回血没有代价, 会破坏这张牌"强效果 + 明确代价"的定位。
        var teammates = combatState.GetTeammatesOf(Owner.Creature).ToHashSet();
        foreach (var c in combatState.Creatures)
        {
            if (!yylNailong.IsNailong(c)) continue;
            if (teammates.Contains(c)) continue;
            await CreatureCmd.Heal(c, 20);
        }
        // 获得 3 炁
        await yylCmd.GainQi(choiceContext, Owner, 3, this, cardPlay.Card);
        // 1 → 2 层无实体
        var intangible = DynamicVars["IntangiblePower"].IntValue;
        if (intangible > 0)
            await PowerCmd.Apply<IntangiblePower>(choiceContext, new[] { Owner.Creature }, intangible, Owner.Creature, cardPlay.Card);
    }
}
