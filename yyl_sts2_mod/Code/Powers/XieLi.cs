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
///         ★2026-11-17 用户定调: 层数必须<b>真实递减并显示在 UI 上</b> —— 此前用隐藏
///         <c>Consumed</c> 计数、<c>Amount</c> 恒定不变, 玩家看到层数不动还以为没生效,
///         具有误导性。现改为直接 <c>PowerCmd.Decrement(this)</c>: 每挡下一次攻击减 1 层,
///         <b>层数即剩余可挡次数</b>, 减到 0 由引擎命令管线自动散去 (照原版 ToricToughnessPower)。
///         只要还剩层, 后续攻击依旧减半, 直到耗尽。
///     </para>
/// </summary>
public sealed class XieLi : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        // ★层数即剩余可挡次数 (真实递减, UI 可见)。无层时不再减伤。
        if (Amount <= 0) return 1m;
        if (target != Owner) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        return 0.5m;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        // 无层(理论上不会走到, 因为减到 0 时命令管线会自动散去) → 直接返回。
        if (Amount <= 0) return;

        // ★必须确认"这一击确实打在自己身上" —— 否则玩家打出空挥/ 打给别人也会消耗层数。
        var hitMe = command.Results
            .SelectMany(result => result)
            .Any(result => result.Receiver == Owner);
        if (!hitMe) return;

        // 真实减 1 层 (照原版 ToricToughnessPower: 走命令管线, 联机同步, 归零自动散去)。
        //   每次 AfterAttack 对应"单个敌人的一次攻击命令", 多段攻击算一次 → 只减 1 层。
        //   减到 0 后引擎自动移除; 若仍有层, 下次攻击继续减半。
        await PowerCmd.Decrement(this);
    }

    /*  ★2026-09-22 用户定调: 卸力改为**不随回合减少**的 buff。
        原先这里在敌方回合收尾时把自己移除 (保护窗口 = 本回合 + 敌方回合),
        导致攒着不用就会白白过期。现已去掉 —— 卸力会一直保留,
        直到真正吃掉对应次数的攻击伤害后由 AfterAttack 散去。 */
}