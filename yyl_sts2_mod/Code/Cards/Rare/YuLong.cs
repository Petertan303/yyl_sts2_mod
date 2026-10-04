using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     驭龙（稀有能力）: 2 费（升级 1 费）。
///     获得[gold]驭龙[/gold]：你造成的攻击对[gold]奶龙[/gold]的伤害提高 50%→75%。
///     <para>
///         机制照搬原版 TRACKING（追踪："虚弱敌人受到的攻击伤害提高 50%"），
///         仅把判定条件从"虚弱"换成"奶龙"。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class YuLong(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public YuLong() : this(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithPower<Powers.YuLong>(50); // 50% → 75%
        WithCostUpgradeBy(-1);            // 2 → 1 费
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Powers.YuLong>(
            choiceContext,
            new[] { Owner.Creature },
            DynamicVars["YuLong"].IntValue,
            Owner.Creature,
            cardPlay.Card);
    }
}
