using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Compatibility;
using MegaCrit.Sts2.Core.ValueProps;

namespace yyl_sts2_mod.Code.Cards.Common;

/// <summary>
///     掌心雷: 1 费, 失去 1 炁, 对单体造成 6 → 8 伤害。
///     <para>
///         TODO: BaseLib 中 <c>PlayerCmd.GainBlock</c> / <c>PlayerCmd.Draw</c> / <c>CreatureCmd.GainBlock</c>
///         等命令的方法名待用户确认 (StS1 习惯名, 在 StS2 BaseLib 中可能不同)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ZhangXinLei(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : ConstructedCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ZhangXinLei() : this(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = 6; // TODO upgrade: would be 8 if IsUpgraded
        // 先失去 1 炁 (走 ILoseQi 钩子, 触发 DanShi)
        await yylCmd.LoseQi(choiceContext, Owner, 1, this, cardPlay.Card);
        await CompatibilityCreatureCmd.Damage(
            choiceContext,
            cardPlay.Target!,
            damage,
            default(ValueProp),
            cardSource: cardPlay.Card,
            cardPlay: cardPlay);
    }
}
