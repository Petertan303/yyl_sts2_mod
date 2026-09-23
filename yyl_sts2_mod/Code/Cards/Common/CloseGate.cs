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
///     封门: 1 费, 获得 5 → 7 点格挡; 若你有金光护体, 改为 9 → 11 点。
///     金光流的普通防御件: 有护体时性价比明显提升, 没护体时也不至于废。
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class CloseGate(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public CloseGate() : this(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        // 格挡 5 → 7。卡面 {Bonus} 用普通 DynamicVar 显示"有金光护体时额外 +4 格挡"的预期值:
        // 普通 var 的 IConvertible 返回 BaseValue, 战斗外也能正确显示 4
        // (CalculatedVar 在战斗外被 BaseLib 强制返回 0, 正是之前显示 0/花括号的根因)。
        WithBlock(5, 2);
        WithVar("Bonus", 4, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 金光加成维持显式计算 (Block 变量无法活 calc); 数值与卡面 {Bonus} 同源 (4)。
        var bonus = Owner.HasPower<GoldenAegis>() ? 4 : 0;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.IntValue + bonus,
            ValueProp.Move, cardPlay);
    }
}
