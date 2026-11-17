using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Uncommon;

/// <summary>
///     返照 (显示名; 类名 BaoShan / Id BAO_SHAN 保持不变 —— 存档按 Id 序列化, 改 Id 会使旧存档失效):
///     2 费, 获得 8 → 11 点格挡。若你有金光护体, 消耗<b>全部</b>金光护体:
///     再获得 8 → 11 点格挡, 且下一次打出攻击牌时, 获得<b>2 倍于消耗量</b>的金光护体
///     (隐藏 power <see cref="YinAegis" />)。
///     <para>
///         ★2026-11-02 (用户设计 v3): 原站桩的"两段格挡"结构整体上移到本卡并升级 ——
///         代价从固定 2 层改为<b>全部</b>, 并新增 2 倍回流 (金光几何翻倍引擎)。
///         站桩因此雪藏 (本卡是其严格上位)。整卡在防御端内部运作:
///         金光 → 即时格挡 + 金光化炁回炉 → 翻倍回流, 无"金光转伤害"的越端问题。
///     </para>
///     <para>
///         强度阀门: ①无金光时只有前半段 (2 费 8 格挡的白板下限, 保留站桩的条件门槛精神);
///         ②每轮翻倍要 2 费 + 一张攻击牌触发; ③烧光后到下一张攻击牌之间金光归零 (裸奔窗口);
///         ④金光化炁的炁收益是对数曲线。命名: 回光返照 —— 放出去的光, 反射回来还更强。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class BaoShan(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public BaoShan() : this(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        // 格挡拆两半 (数值承自站桩: 8+8=16 / 11+11=22), 第二段以"消耗全部金光"为条件。
        WithBlock(8, 3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        yylVfx.OnCreatureRaised(Owner.Creature, "vfx/vfx_block", 1f/3f); // 打出特效: 格挡火花
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var half = DynamicVars.Block.IntValue;
        await CommonActions.CardBlock(this, cardPlay);

        // 后半段: 消耗全部金光换取等量格挡 (统一入口, 「金光化炁」按失去层数回炉成炁),
        //   再把 2 倍于消耗量的金光投放到下一次攻击牌 (引金光, Counter 可叠加)。
        //   无金光则只有前半段 (2 费 8 格挡的白板下限)。
        var layers = (int)(Owner.Creature.GetPower<GoldenAegis>()?.Amount ?? 0m);
        if (layers <= 0) return;
        await yylAegis.Consume(choiceContext, this, layers);
        await CreatureCmd.GainBlock(Owner.Creature, half, ValueProp.Move, cardPlay, false);

        await PowerCmd.Apply<YinAegis>(
            choiceContext, new[] { Owner.Creature }, layers * 2m, Owner.Creature, cardPlay.Card);
    }
}
