using MegaCrit.Sts2.Core.Entities.Powers;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     金光化炁: 每<b>消耗</b> 1 层[gold]金光护体[/gold]，获得 <c>Amount</c> 点[gold]炁[/gold]。
///     <para>
///         常态下金光护体是纯消耗品（掌心雷 / 白长虫 / 五雷正法用它换每段 +1 伤害），
///         有了本能力后"付出去的金光"会回炉成炁，把防御资源与进攻资源真正接通。
///     </para>
///     <para>
///         挂钩点在 <see cref="yylAegis.Consume" />（金光护体的统一消耗入口），
///         不在本类里 —— 因为"消耗"是发生在卡牌结算中的动作，Power 自身观察不到。
///     </para>
/// </summary>
public sealed class JinGuangHuaQi : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>层数 = 每消耗 1 层金光护体获得的炁量。</summary>
    public override PowerStackType StackType => PowerStackType.Counter;
}
