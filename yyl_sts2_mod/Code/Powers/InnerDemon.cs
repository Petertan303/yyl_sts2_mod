using System.Threading.Tasks;
using BaseLib.Audio;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     心魔: 大瓶黄桃罐头 (NlCan2) 的奶龙标记。
///     "Applier 攻击击杀 Owner 时, Applier 回复 6 点生命" (奶龙标记的 6 血版)。
///     <para>
///         ⚠ <c>Type = PowerType.None</c> —— 必须与 <see cref="NailongMark" /> 完全对齐:
///         奶龙/心魔是**身份标记**而非负面, 中性类型才不会被人工制品抵消、
///         也不会被涤荡/拔罐/舒筋等"清除负面"效果误删 (否则心魔一掉,
///         所有奶龙联动卡对持有者失效)。之前是 Debuff, 已修正。
///     </para>
/// </summary>
public sealed class InnerDemon : yylPowerModel
{
    public override PowerType Type => PowerType.None;
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    /*  ★2026-09-22 修复: 击杀心魔不回血 (与 NailongMark 同因)。
        去掉了 `_applierIsAttacking` 守卫 —— 该标记恒为 false, 导致回血永远不触发
        (详见 NailongMark.cs 中的说明)。改为「带心魔标记者死亡 → 其施加者回血」。 */
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (Applier == null || wasRemovalPrevented || creature != Owner) return;
        if (Applier.IsAlive)
        {
            await CreatureCmd.Heal(Applier, 6m);
        }
    }
}
