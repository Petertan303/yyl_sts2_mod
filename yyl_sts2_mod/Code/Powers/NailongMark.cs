using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

public sealed class NailongMark : yylPowerModel
{
    public override PowerType Type => PowerType.None; 
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    
    /*  ★2026-09-24 改为 FEED/狂宴 式"谁斩杀谁触发":
        原实现给「标记施加者(Applier)」回血 —— 但在联机里, 若队友斩杀了这只奶龙,
        回血却给了标记拥有者而非斩杀者, 不符合直觉。
        现在改为给「造成致命伤的来源(Dealer)」回血: 取战斗历史里本 creature 最近一次
        受击记录 (DamageReceivedEntry) 的 Dealer 作为斩杀者, 联机里队友斩杀也会给自己回血。
          ① 历史条目在真实掉血后才落账, 因此 AfterDeath 触发时致命一击的
             DamageReceivedEntry 必然已在 CombatManager.Instance.History.Entries 中。
          ② 单机下 Dealer 就是玩家自身, 行为与旧版完全一致。
          ③ 兜底: 极少数"非伤害清除"(如被状态直接移除)没有受击记录时, 退化给标记施加者,
             避免回血凭空消失。 */
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner) return;

        // 斩杀者 = 本 creature 最近一次受击记录 (致命一击) 的 Dealer。
        var killer = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(e => e.Receiver == creature)
            .LastOrDefault()?.Dealer;

        // 兜底: 无受击记录时退化给标记施加者 (保持原行为)。
        if (killer == null) killer = Applier;

        if (killer != null && killer.IsAlive)
        {
            await CreatureCmd.Heal(killer, 3m);
        }
    }
}
