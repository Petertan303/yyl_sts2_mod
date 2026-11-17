using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     老农功: 0 费<b>消耗</b>稀有技能, 获得 1 层[炁婴] —— 危急时刻由炁婴接管身体,
///     额外获得一个由系统自动出牌的回合。
///     <para>
///         ★2026-11-16 用户定调: 费用 1 → <b>0</b>, 并补上<b>消耗</b>关键字。
///         升级效果不变(仍是附加「保留」), 因此升级后是「0 费 / 消耗 / 保留」。
///         0 费白嫖一个额外回合过强, 消耗保证每场战斗只能打出一次。
///     </para>
///     <para>
///         原作里冯宝宝把这门功法叫"老农功", 说练到深处会有"炁婴"接管身体打架,
///         本体反而失去意识 —— 正好对应"这一回合不由你操作"。
///         机制照搬 LexNinja2 的「罗汉分身」。
///     </para>
///     <para>
///         [2026-11-08 用户定调] 配套原版「疾影」(<see cref="BlurPower" />): 打出本卡时给
///         自己 1 层疾影 —— 原版定义是"下回合开始时不会移除格挡"。老农功在当前回合打出,
///         下一个回合正是炁婴接管的额外回合, 1 层刚好把格挡从当前回合带进那个额外回合。
///         疾影由引擎在下回合开始阻止清格挡后自动散去, 不会永久保留格挡。
///     </para>
///     <para>
///         ★ 施加放在打牌入口而不是炁婴的 <c>AfterApplied</c>: 后者只在首次施加时触发,
///         已有炁婴再叠层时不进钩子, 会导致重复打出本卡时疾影静默失效。
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
        // ★2026-11-16: 0 费不再降费; 升级只附加「保留」, 效果保持不变。
        WithKeywords(CardKeyword.Exhaust);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
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

        // 疾影 = 1 次「下回合开始不移除格挡」, 覆盖到炁婴接管的额外回合。
        // 整体 try/catch: 打牌流程抛异常会冻结整场战斗。
        try
        {
            await PowerCmd.Apply<BlurPower>(
                choiceContext,
                new[] { Owner.Creature },
                1m,
                Owner.Creature,
                cardPlay.Card);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 老农功施加疾影失败, 已忽略: {e.Message}");
        }
    }
}
