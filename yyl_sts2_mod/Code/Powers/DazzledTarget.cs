using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     瞪视: <b>本回合你对该敌人的伤害翻倍</b> (由「瞪视」给予, 2026-10-03)。
///     <para>
///         与常驻增伤不同, 这是<b>单目标</b>的伤害乘区: 只对被盯住的那一个敌人翻倍,
///         换目标就不生效。被盯目标真的死亡后本power 立即自毁(不白占乘区)。
///     </para>
///     <para>
///         时限: 回合结束由「抱山」等调用方移除, 或目标死亡时自毁(见 <see cref="AfterDeath" />)。
///         Power 本身不自动过期 —— 引擎只在回合开始/结束处理 Duration 类 power,
///         所以清理由施加方负责(卡牌在 <c>AfterSideTurnEnd</c> 无法可靠挂载, 改由目标死亡兜底)。
///     </para>
/// </summary>
public sealed class DazzledTarget : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>被盯住的敌人。战斗内对象, 不持久化。</summary>
    public Creature? Target { get; private set; }

    public DazzledTarget(Creature target)
    {
        Target = target;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (Target == null) return 1m;
        if (target != Target) return 1m;            // 只对被盯的那一个生效
        if (dealer != Owner) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        return 2m;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        // 被盯的敌人真的死了 → 增伤立即失效, 避免"打空气刷伤害"。
        if (creature != null && creature == Target && Target.IsDead)
        {
            await PowerCmd.Remove(this);
            return;
        }

        await Task.CompletedTask;
    }
}