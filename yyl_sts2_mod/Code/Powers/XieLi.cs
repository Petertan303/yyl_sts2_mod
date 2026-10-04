using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Patches;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     卸力: 每层挡一次攻击, 你受到该次攻击的伤害<b>减半</b>, 挡完即消耗一层 (由「化劲」给予)。
///     <para>
///         实现要点: 伤害预览 (敌方意图上的数字) 也会走 ModifyDamage 管线,
///         因此不能在修改器里"用掉"层数 —— 否则预览先消耗、真打来时反而不减伤。
///         这里改为在 <see cref="AfterAttack" /> 里确认"这一击确实打在自己身上"后才消耗层数,
///         预览不会触发该钩子。
///     </para>
///     <para>
///         ★2026-10-02 修正叠层丢失: 原实现用**一个全层共享的 <c>_used</c> 布尔**,
///         被打中一次就 <c>PowerCmd.Remove(this)</c> 移除整个 power ——
///         叠 3 层时一次攻击全消失(用户实测反馈)。
///         改为<b>按层消耗</b>: 用 <c>_consumed</c> 记录已消耗的层数,
///         <c>Amount</c> 恒等于总层数(卡面显示的层数 = 还能挡几次),
///         剩余层数耗尽才移除整个 power。
///     </para>
/// </summary>
public sealed class XieLi : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>已被消耗掉的层数 (= 挡掉的攻击次数)。</summary>
    [SavedProperty]
    private int Consumed { get; set; }

    /// <summary>还剩几层可用(未消耗的层数)。</summary>
    private int Remaining => Amount - Consumed;

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        // ★按"是否还有未消耗的层"判定, 不再用全局布尔 —— 叠层时每层各挡一次。
        if (Remaining <= 0) return 1m;
        if (target != Owner) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        return 0.5m;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        // 已被打空(理论上不会走到, 因为空时会Remove) → 直接返回。
        if (Remaining <= 0) return;

        // ★必须确认"这一击确实打在自己身上" —— 否则玩家打出空挥/ 打给别人也会消耗层数。
        var hitMe = command.Results
            .SelectMany(result => result)
            .Any(result => result.Receiver == Owner);
        if (!hitMe) return;

        // 消耗一层。
        Consumed++;

        // 层数耗尽 → 移除整个 power(引擎 SetAmount/Amount 归零不会自动移除, 必须显式 Remove)。
        if (Remaining <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }

    /*  ★2026-09-22 用户定调: 卸力改为**不随回合减少**的 buff。
        原先这里在敌方回合收尾时把自己移除 (保护窗口 = 本回合 + 敌方回合),
        导致攒着不用就会白白过期。现已去掉 —— 卸力会一直保留,
        直到真正吃掉对应次数的攻击伤害后由 AfterAttack 散去。 */
}