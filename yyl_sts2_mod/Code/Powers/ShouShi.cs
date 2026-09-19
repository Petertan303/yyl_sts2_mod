using MegaCrit.Sts2.Core.Entities.Powers;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     守势: 金光护体每层<b>额外</b>减伤 Amount 点 (基础 2 → 3), 但你的炁<b>不再提供伤害加成</b>。
///     <para>
///         一张"切换攻防"的宣言式能力: 炁(攻)与金光护体(防)本来是两条腿,
///         守势把炁的攻击收益换成金光的防御收益, 让"只堆金光"成为一条可行路线。
///         减伤与取消增伤分别在 <see cref="GoldenAegis" /> 与 <see cref="Qi" /> 里读取本 Power。
///     </para>
/// </summary>
public sealed class ShouShi : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>每层金光护体在基础减伤之外额外减免的伤害。</summary>
    public const decimal BaseBonus = 1m;

    /// <summary>升级后每层金光护体额外减免的伤害。</summary>
    public const decimal UpgradeBonus = 2m;
}
