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
///     卸力: 你受到的<b>第一次攻击</b>伤害减半, 之后自行散去 (由「化劲」给予)。
///     <para>
///         实现要点: 伤害预览 (敌方意图上的数字) 也会走 ModifyDamage 管线,
///         因此不能在修改器里"用掉"标记 —— 否则预览先消耗、真打来时反而不减伤。
///         这里改为在 <see cref="AfterAttack" /> 里确认"这一击确实打在自己身上"后才失效,
///         预览不会触发该钩子。玩家回合结束时若仍未触发, 一并散去。
///     </para>
/// </summary>
public sealed class XieLi : yylPowerModel, IModifyDamageMultiplicative
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _used;

    public decimal ModifyDamageMultiplicativeCompability(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (_used) return 1m;
        if (target != Owner) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        return 0.5m;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (_used) return;
        var hitMe = command.Results
            .SelectMany(result => result)
            .Any(result => result.Receiver == Owner);
        if (!hitMe) return;

        _used = true;
        await PowerCmd.Remove(this);
    }

    /*  ★2026-09-22 用户定调: 卸力改为**不随回合减少**的 buff。
        原先这里在敌方回合收尾时把自己移除 (保护窗口 = 本回合 + 敌方回合),
        导致攒着不用就会白白过期。现已去掉 —— 卸力会一直保留,
        直到真正吃掉一次攻击伤害后由 AfterAttack 散去 (仍是一次性)。 */
}
