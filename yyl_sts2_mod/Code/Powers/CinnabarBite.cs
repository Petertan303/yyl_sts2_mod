using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Events;

namespace yyl_sts2_mod.Code.Powers;

/// <summary>
///     丹噬: <b>失去炁时</b>, 对所有敌人施加「本次实际失去的炁量 × 层数」层[中毒]。
///     <para>
///         ★2026-11-08 用户定调: 触发端由「获得炁」改为「失去炁」(耗炁侧收口),
///         与温养(获得炁→格挡) 形成攻防对调; 卡牌层数 1 → 2 提升为 2 → 3。
///         数值公式不变 (实际失炁量 × 层数), 中毒按原版规则在敌方回合开始结算、每回合减 1 层,
///         不吃炁/力量/易伤加成。
///     </para>
///     <para>
///         Hook-based: 实现 <see cref="ILoseQi" />, 在每次失去炁后作为后续副作用结算。
///         <c>yylCmd.LoseQi</c> 会把失炁量截断到当前持有的炁量, 所以钩子收到的
///         <c>modifiedAmount</c> 是<b>实际</b>失去量, 不会因"失去所有炁"而透支。
///     </para>
/// </summary>
public sealed class CinnabarBite : yylPowerModel, ILoseQi
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>卡牌默认给予的层数 (基础 2 层, 升级后 +1 层 = 3 层)。</summary>
    public const int DefaultAmount = 2;

    /// <summary>升级时增加的层数。</summary>
    public const int UpgradeAmount = 1;

    public int ModifyQiLoss(Player player, int amount)
    {
        // 不参与数值修改, 只挂 AfterModifyingQiLoss 后续结算。
        return amount;
    }

    public async Task AfterModifyingQiLoss(
        PlayerChoiceContext ctx,
        Player player,
        int originalAmount,
        int modifiedAmount)
    {
        if (modifiedAmount <= 0 || Amount <= 0) return;

        // ★钩子遍历全场监听者, 联机时队友失炁也会走到这里 (与 GildedMirror 同理)。
        if (player != Owner.Player) return;

        try
        {
            var combatState = player.Creature.CombatState;
            if (combatState == null) return;

            // ★中毒层数 = 本次实际失去的炁量 × 丹噬层数。
            var poison = (decimal)modifiedAmount * Amount;

            // 先快照敌人列表: 逐个施加, 单个目标失败不影响其余目标。
            var enemies = (combatState.HittableEnemies ?? [])
                .Where(e => e != null && e.IsAlive)
                .ToList();

            foreach (var enemy in enemies)
            {
                try
                {
                    // 中毒是负面状态, PowerCmd 无 CanApply 检查; 逐个包 try/catch 防止
                    // 某个目标不可施加时把整次失炁的后续结算打断。
                    await PowerCmd.Apply<PoisonPower>(ctx, enemy, poison, player.Creature, null);
                }
                catch (Exception ex)
                {
                    MainFile.Logger.Warn($"[yyl_sts2_mod] 丹噬施加中毒失败: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // 钩子绝不向战斗管线抛异常 (会冻结整场战斗)。
            MainFile.Logger.Warn($"[yyl_sts2_mod] 丹噬结算失败: {ex.Message}");
        }
    }
}
