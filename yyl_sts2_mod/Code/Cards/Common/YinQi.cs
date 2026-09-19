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
///     引炁: 1 费, 获得 1 → 2 层[引炁] —— 下一次打出攻击牌时获得等同层数的炁。
///     把产炁挪到攻击之后 (先出招、再攒炁), 与马步(立刻到账)、蓄势(下回合到账) 错开节奏。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class YinQi(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public YinQi() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithPower<Powers.YinQi>(1, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["YinQi"].IntValue;
        await PowerCmd.Apply<Powers.YinQi>(choiceContext, new[] { Owner.Creature }, amount, Owner.Creature, cardPlay.Card);
    }
}
