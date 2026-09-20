using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     老农功: 0 费稀有技能, 获得 1 层[炁婴] —— 危急时刻由炁婴接管身体,
///     额外获得一个由系统自动出牌的回合。
///     <para>
///         原作里冯宝宝把这门功法叫"老农功", 说练到深处会有"炁婴"接管身体打架,
///         本体反而失去意识 —— 正好对应"这一回合不由你操作"。
///         机制照搬 LexNinja2 的「罗汉分身」。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
#pragma warning disable STS004
public class PeasantDrill(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public PeasantDrill() : this(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithPower<QiInfant>(1);
        // WithCostUpgradeBy(-1);
        WithKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var amount = DynamicVars["QiInfant"].IntValue;
        await PowerCmd.Apply<QiInfant>(
            choiceContext,
            new[] { Owner.Creature },
            amount,
            Owner.Creature,
            cardPlay.Card);
    }
}
