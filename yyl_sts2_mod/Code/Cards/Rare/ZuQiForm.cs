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
///     祖炁形态（形态卡）: <b>3 费</b>，<b>升级不降费</b>（原版形态卡共同特征），
///     升级只把炁的线性系数从 8% 提到 12%。
///     <para>
///         效果: 炁的增伤<b>不再收益递减</b> —— 每 1 点炁使造成的伤害提高
///         8%→12%（线性、无衰减），取代默认的对数曲线。
///         对照: 20 炁时 默认 1.83x / 祖炁形态 2.6x(8%) ~ 3.4x(12%)；
///         40 炁时 默认 2.0x / 祖炁形态 4.2x ~ 5.8x。
///     </para>
///     <para>
///         设计备注: 原名「先天一炁」改作祖炁形态 —— 形态卡需符合「xx形态」命名与
///         3 费不降费的原版惯例；「先天一炁」这个名字留给遗物使用。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ZuQiForm(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ZuQiForm() : this(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        // 升级只增强系数 8% → 12%，**不降费**（对标原版形态卡）。
        WithPower<Powers.ZuQiForm>(8, 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Powers.ZuQiForm>(
            choiceContext,
            new[] { Owner.Creature },
            DynamicVars["ZuQiForm"].IntValue,
            Owner.Creature,
            cardPlay.Card);
    }
}
