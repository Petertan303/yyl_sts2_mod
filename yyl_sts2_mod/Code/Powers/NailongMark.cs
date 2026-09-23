using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

public sealed class NailongMark : yylPowerModel
{
    public override PowerType Type => PowerType.None; 
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    
    /*  ★2026-09-22 修复: 击杀奶龙不回血。
        原实现用 `_applierIsAttacking` 守卫 —— 靠 BeforeAttack 置位、AfterAttack 清位,
        要求"击杀正好发生在施加者攻击的窗口内"才回血。实测该标记恒为 false, 回血被整段跳过:
          ① 死亡回调很可能排在 AfterAttack 之后, 读到时标记已被清掉;
          ② 本力量挂在**受击的奶龙**身上, 若引擎的 BeforeAttack 只作用于攻击者自身的力量,
             这个标记从一开始就不会被置位。
        两种情形都无法靠调整钩子顺序稳妥解决, 因此直接去掉该守卫:
        改为「带标记的奶龙死亡 → 其施加者回血」(施加者仍须存活), 与遗物文案一致。 */
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (Applier == null || wasRemovalPrevented || creature != Owner) return;
        if (Applier.IsAlive)
        {
            await CreatureCmd.Heal(Applier, 3m);
        }
    }
}
