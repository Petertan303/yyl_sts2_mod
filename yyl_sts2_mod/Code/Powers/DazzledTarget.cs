using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Cards.Uncommon;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     瞪视: <b>本回合你对该敌人的伤害翻倍</b> (由「瞪视」给予,挂在<b>被打的敌人</b>身上)。
///     <para>
///         ★★ <b>2026-10-07 重构</b>——此前两天返工两次的根源都在「手动 new / 手动 ToMutable」:
///         <para/>
///         【旧设计的问题】power 挂在<b>玩家</b>身上, 需要记住"我盯的是哪个敌人"
///         ⇒ 必须有字段存目标 ⇒ 就得在 <c>PowerCmd.Apply</c> 之前给实例注入自定义数据
///         ⇒ 只能手动 <c>ModelDb.Power&lt;T&gt;().ToMutable(...)</c>, 于是连踩两个坑:
///         <list type="number">
///             <item>手动 <c>new</c> → <c>DuplicateModelException</c>(ModelDb 已有规范单例);</item>
///             <item>手动 <c>ToMutable(1)</c> → <c>SetAmount</c> 里 <c>Owner</c> 还没赋值 →
///             <c>NullReferenceException</c>(这也是引擎自己只用无参 <c>ToMutable()</c> 的原因)。</item>
///         </list>
///         【新设计】power 改挂在<b>被打的那个敌人</b>身上, "目标是谁"由 <see cref="PowerModel.Owner" />
///         天然承载, <b>彻底不需要自定义字段</b> ⇒ 可以直接用全库统一的泛型
///         <c>PowerCmd.Apply&lt;DazzledTarget&gt;(...)</c>, 由引擎负责实例化与查重。
///     </para>
///     <para>
///         判定因此极简:「攻击者是我方」且「被打的目标是我」⇒ 伤害 ×(1+层数)。
///         <b>每层 +1 倍(线性)</b>而非 2^层数 —— 后者叠 3 层就是 ×8, 过于失控。
///         敌人死亡时power 随其身上的 power 一起被引擎清除, 无需自毁逻辑。
///     </para>
///     <para>
///         时限「本回合」: 由 <see cref="AfterSideTurnEnd" /> 在我方回合结束时移除自身
///         (范式照 <c>Powers/HeavenlyBurden</c> —— 同为"回合结束时结算/清除"的 power)。
///     </para>
/// </summary>
public sealed class DazzledTarget : yylPowerModel
{
    /// <summary>
    ///     ★2026-11-17 用户定调: 瞪视是给敌人挂的<b>负面状态</b>(让它多挨打), 改 Buff → Debuff。
    ///     这样人工制品/净化类若针对 debuff 会正确作用, 语义也与"易伤"一致。
    /// </summary>
    public override PowerType Type => PowerType.Debuff;

    /// <summary>
    ///     <see cref="PowerStackType.Counter" />: 每次被瞪视叠 1 层, 攻击者对它每层多打一倍。
    ///     ★2026-11-17 起每打出一张攻击牌消耗 1 层(见 <see cref="AfterCardPlayed" />), 层数 = 剩余生效次数。
    /// </summary>
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>无参构造 (引擎 ModelDb.Init 用 Activator.CreateInstance 建规范单例时要求)。</summary>
    public DazzledTarget()
    {
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        // 我是被盯的那只怪(target == Owner), 且攻击者是我方 ⇒ 增伤。
        // ★ 诊断日志(2026-10-07 朋友反馈"卡在待打出区/buff 不生效"时加, 确认后删)
        MainFile.Logger.Info(
            $"[DazzledTarget] hit: owner={(Owner == null ? "null" : Owner.Name)} " +
            $"target={(target == null ? "null" : target.Name)} " +
            $"dealer={(dealer == null ? "null" : dealer.Name)} amount={Amount} props={props}");

        if (Owner == null || target != Owner) return 1m;
        if (dealer == null || dealer == target) return 1m;      // 自己打自己不触发
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        // 每层 +1 倍(线性), 而非 2^Amount —— 后者叠 3 层就是 ×8, 过于失控。
        return 1m + Amount;
    }

    /// <summary>
    ///     ★2026-11-17 用户定调: 瞪视改为<b>只对下一张攻击牌生效、翻倍一次即消耗</b>。
    ///     我方每打出一张攻击牌(且目标正是被盯的 Owner)就减 1 层, 归零时引擎自动移除。
    ///     <para>
    ///         跳过施加牌自身: 「瞪视」这张卡是攻击牌、目标也是 Owner, 但它的伤害在
    ///         <c>OnPlay</c> 里先结算、power 后挂(见 DengShi.cs), AfterCardPlayed 时
    ///         若把自身也算进去就会刚挂上立刻被消费掉 —— 用 <c>card is DengShi</c> 排除。
    ///     </para>
    ///     <para>
    ///         范式照 <c>NextSkillDoubleBlock.AfterCardPlayed</c>: 在卡牌结算完成后扣层,
    ///         不在同步乘区钩子里改层; 用 <c>PowerCmd.Apply(..., -1m, ...)</c> 走命令管线联机同步。
    ///     </para>
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Amount <= 0) return;
        if (cardPlay?.Card == null) return;
        // 只认我方打出的攻击牌。
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (cardPlay.Player?.Creature == null || !cardPlay.Player.Creature.IsPlayer) return;
        // 施加牌自身不算(先伤害后挂 power, 否则刚挂就被消费)。
        if (cardPlay.Card is DengShi) return;
        // 目标必须正是被瞪的那个敌人。
        if (cardPlay.Target != Owner) return;

        await PowerCmd.Apply(choiceContext, this, Owner, -1m, cardPlay.Player.Creature, cardPlay.Card);
    }
}