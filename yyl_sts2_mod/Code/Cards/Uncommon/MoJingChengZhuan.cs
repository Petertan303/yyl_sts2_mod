using System.Threading.Tasks;
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

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     磨镜成砖: 0 费, 失去 3 点生命, 获得 8 → 12 点格挡;
///     在下 2 个回合开始时, 各获得 8 → 12 点格挡 (power <see cref="JiZhuan" />)。
///     <para>
///         ★2026-11-02 (用户设计): 名字是禅宗公案「磨砖成镜」的反转 —— 别人磨砖妄图成镜
///         (南岳怀让点化马祖道一的公案, 原意"徒劳"), 我把镜磨成砖: 不要虚光, 要实料。
///         失去生命 (血肉) 换来分期格挡 (砖石), 防御总产出 8 + 8×2 = 24 (升级 12 + 12×2 = 36)。
///         代价与产出同构: 镜 (光华) 磨成砖 (实料)。
///     </para>
///     <para>
///         「积砖」power 逻辑完全照原版 ToricToughnessPower (用户指定):
///         格挡被清空时 (回合开始) 获得设定值格挡并层数 -1, 减到 0 自动散去;
///         格挡数量经 SetBlock 参数化 (原版固定 5, 此处 8 → 12)。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class MoJingChengZhuan(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public MoJingChengZhuan() : this(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        WithBlock(8, 4);                // 即时格挡 8 → 12
        WithVar("EchoBlock", 8, 4);     // 积砖每回合格挡 8 → 12
        // 失去 3 点生命: 固定代价 (用户定调, 升级不变), 见 OnPlay。
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this);
        yylVfx.OnCreatureRaised(Owner.Creature, "vfx/vfx_block", 1f/3f); // 打出特效: 格挡火花

        // 失去 3 点生命 (固定代价): yylCmd.LoseHp 不走伤害管线 —— 不吃格挡/金光/炁增伤
        //   (金光的减伤钩子对 dealer == null 的固定代价不生效, 燃炁同款)。
        yylCmd.LoseHp(Owner.Creature, 3);

        // 即时格挡 (WithBlock 变量)。
        await CommonActions.CardBlock(this, cardPlay);

        // 积砖: 下 2 个回合开始时各获得 EchoBlock 点格挡 (逻辑照原版 ToricToughnessPower)。
        await PowerCmd.Apply<JiZhuan>(choiceContext, new[] { Owner.Creature }, 2, Owner.Creature, cardPlay.Card);
        if (Owner.Creature.GetPower<JiZhuan>() is { } brick)
            brick.SetBlock(DynamicVars["EchoBlock"].IntValue);
    }
}
