using MegaCrit.Sts2.Core.Entities.Powers;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     起始装备初始化标记 —— <b>隐藏的幂等标记</b>，用来判断"本场战斗的开局装备是否已经布置过"。
///     <para>
///         ★ <b>为什么不能用 <c>PlayerCombatState.TurnNumber == 1</c> 当首次判据</b> (2026-10-19):
///         联机对局里各玩家的战斗起始回合数可能**错开**（谁先手/掉线重连都会导致差异），
///         于是"回合数 == 1"这个守卫会在联机下整体失配 —— 黄桃罐头 / 大瓶黄桃罐头的
///         <c>BeforeHandDraw</c> 整段被 <c>return</c> 掉：开局拿不到初始炁、拿不到奶龙标记，
///         于是<b>所有耗炁卡（太极 / 散炁 / 站桩 / 天火 …）在联机里全部无法生效</b>。
///         项目里 Identify 的注释其实已经记录了这个隐患，只是当时没去修守卫本身。
///     </para>
///     <para>
///         幂等标记走 Power（战斗状态，各端一致，存档/联机都不会错），
///         而不是任何回合计数或本地标记。
///     </para>
/// </summary>
public sealed class StarterSetup : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>对玩家完全隐藏（不占 buff 槽位、不显示图标），与 <c>CardGlowDriver</c> 同款。</summary>
    protected override bool IsVisibleInternal => false;
}