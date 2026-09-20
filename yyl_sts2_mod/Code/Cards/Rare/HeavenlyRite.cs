using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     天师度 卡牌: 2 费, 获得 30 → 40 炁 + 5 层金光护体, 同时给予 天师度 Power
///     (回合一结束 -10 炁 / 层)。类比 Wraith Form: 强力正向 + 持续负面。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class HeavenlyRite(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public HeavenlyRite() : this(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<Qi>(30, 10);
        WithPower<GoldenAegis>(5, 0);
        WithPower<HeavenlyBurden>(1, 0);
        // 仅用于卡面显示: 天师度每回合抽走的炁
        WithPower<Qi>("BurdenQi", 10, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        // 正向: 获得 30 → 40 炁 + 5 层金光护体
        await yylCmd.GainQi(choiceContext, Owner, DynamicVars["Qi"].IntValue, this, cardPlay.Card);
        await PowerCmd.Apply<GoldenAegis>(choiceContext, new[] { Owner.Creature },
            DynamicVars["GoldenAegis"].IntValue, Owner.Creature, cardPlay.Card);
        // 负面: 给予天师度 debuff Power
        await PowerCmd.Apply<HeavenlyBurden>(choiceContext, new[] { Owner.Creature }, 1, Owner.Creature, cardPlay.Card);
    }
}
