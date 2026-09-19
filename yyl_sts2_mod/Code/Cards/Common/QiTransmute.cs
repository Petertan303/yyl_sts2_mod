using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     炁化金光: 1 费, 失去 1 点炁, 获得 1 → 2 层金光护体, 并获得 6 → 9 点格挡。
///     炁(攻击端)与金光护体(防御端)之间的转换器。
///     [rule 2026-09-18] 耗炁卡统一判定: 炁不足时无额外效果 (不失去炁, 也不获得护体/格挡)。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class QiTransmute(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public QiTransmute() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithBlock(6, 3);
        WithPower<GoldenAegis>(1, 1);
        // 仅用于卡面显示: 这张卡要花掉的炁
        WithPower<Qi>("QiLoss", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 炁不足 1 时无额外效果 (耗炁卡统一判定)。
        if (Owner.Creature.GetPower<Qi>()?.Amount >= 1)
        {
            // 先失去炁(让丹噬之类的失炁 follow-up 触发)
            await yylCmd.LoseQi(choiceContext, Owner, 1, this, cardPlay.Card);
            // 再获得护体与格挡
            var aegis = DynamicVars["GoldenAegis"].IntValue;
            if (aegis > 0)
                await PowerCmd.Apply<GoldenAegis>(choiceContext, new[] { Owner.Creature }, aegis, Owner.Creature, cardPlay.Card);
            await CommonActions.CardBlock(this, cardPlay);
        }
    }
}
