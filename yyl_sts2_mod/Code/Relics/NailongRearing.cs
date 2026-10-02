using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Patches;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     养龙 (稀有遗物): 每击杀一个[奶龙], 本遗物 +1 层 (最多 20 层);
///     每层使你对[奶龙]造成的伤害 <b>+5%</b> (乘法), 满层 20 层 = +100%。
///     <para>
///         [2026-09-26 调整] 原为"每层固定 +1 点伤害"，改为百分比乘法 ——
///         固定加法在后期大伤害牌面前几乎无感，改成乘法后层数才真正产生成长体感。
///     </para>
///     <para>
///         跨战斗累计的成长型遗物 —— 起始遗物「黄桃罐头」会把所有敌人视作奶龙,
///         所以层数实际上就是"这场旅途中累计击杀数", 属于局外成长。
///     因为计数器是永久的, 上限必须存在, 否则后期会变成一回合秒杀。
///     <para>
///         [2026-10-02] 计数器从 <c>StackCount</c> 换成自有 <c>[SavedProperty] Kills</c>:
///         StackCount 既不参与卡面显示(读的是 DisplayAmount), 也不参与存档(无
///         [SavedProperty]) —— 对一个跨战斗成长遗物两头都不成立。
///     </para>
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class NailongRearing : yylRelicModel, IModifyDamageMultiplicative
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    /// <summary>层数上限。</summary>
    public const int MaxStacks = 20;

    /// <summary>每层对奶龙的伤害加成：每层 +5%（满 20 层 = 伤害翻倍）。</summary>
    public const decimal PercentPerStack = 0.05m;

    public override bool IsStackable => true;

    /*  ★★ 2026-10-02 双修「层数涨一下立刻归零」—— 两个独立缺陷, 症状叠在一起:

      ① 显示层数读的是 DisplayAmount, 不是 StackCount。
         引擎 NRelicInventoryHolder.RefreshAmount:
           `_amountLabel.SetTextAutoSize(_relic.Model.DisplayAmount.ToString())`
         而 RelicModel.DisplayAmount 默认 `=> 0`。原版计数类遗物(OrnamentalFan 等)
         都覆写 DisplayAmount 并在变化时调 InvokeDisplayAmountChanged() 触发刷新。
         我们从未覆写 → 标签恒为 0。

      ② StackCount 本身不是存档字段。RelicModel.ToSerializable() 走
         `SavedProperties.From(this)`, 而 StackCount 没有 [SavedProperty] 特性,
         引擎里也只有 IncrementStackCount() 会写它 —— 读档/跨战斗后必然回到初始值 1。
         养龙是设计上的「跨战斗累计成长遗物」, 用 StackCount 存是错的。

      ⇒ 改为自有 [SavedProperty] 字段 _kills 持久化, 层数 = _kills + 1,
        DisplayAmount 覆写为它, 存档由引擎 SavedProperties 负责。 */
    public override bool ShowCounter => true;

    public override int DisplayAmount => KillCount;

    /// <summary>累计击杀奶龙数 (= 层数 - 1)。用 [SavedProperty] 才能跨战斗/读档保留。</summary>
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public int Kills
    {
        get => _kills;
        private set
        {
            AssertMutable();
            _kills = value;
        }
    }

    private int _kills;

    /// <summary>当前层数 (至少 1)。</summary>
    public int KillCount => 1 + _kills;

    /*  用 BeforeDeath 而不是 AfterDeath:
        到了 AfterDeath, 怪物身上的能力(奶龙标记/心魔)已经被清掉了,
        HasPower<T>() 查不到, 判定永远失败、层数永远不涨。
        注意 BeforeDeath 触发时怪物 IsAlive 已经是 false, 因此这里必须用
        IsNailongMarked(只看标记、不要求存活), 不能用 IsNailong(要求 IsAlive:true),
        否则判定永远失败、层数永远不涨。
        ⚠ BeforeDeath 来自 AbstractModel(不是 RelicModel 自己的钩子 —— RelicModel 只
          声明了 AfterObtained/AfterRemoved), 所以 override 得到编译通过且引擎会调用。 */
    public override Task BeforeDeath(Creature creature)
    {
        if (!yylNailong.IsNailongMarked(creature)) return Task.CompletedTask;
        if (KillCount >= MaxStacks) return Task.CompletedTask;

        Kills = _kills + 1;
        // 主动通知 UI 刷新计数器 (RefreshAmount 只订阅 DisplayAmountChanged)。
        InvokeDisplayAmountChanged();
        Flash();
        return Task.CompletedTask;
    }

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner.Creature) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        if (!yylNailong.IsNailong(target)) return 1m;

        // 每层 +5%，层数按上限钳制（计数器永久，无上限后期会失控）。
        return 1m + PercentPerStack * Math.Min(KillCount, MaxStacks);
    }
}
