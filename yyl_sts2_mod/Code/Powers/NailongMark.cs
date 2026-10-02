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
    
    /*  ★2026-09-24 改为 FEED/狂宴 式"谁斩杀谁触发"后, 用户进一步收紧:
        回血仅限「持有黄桃罐头/大瓶黄桃罐头的人」亲手斩杀奶龙时触发, 其他人斩杀不回血。
          - NailongMark 的 Applier 就是给这只怪物挂标记的遗物持有者
            (PeachCan / NlCan2 都通过 yylNailong.ApplyMark(..., applier = Owner.Creature) 施加)。
          - 斩杀者 = 本 creature 最近一次受击记录 (致命一击) 的 Dealer。
          - 仅当 Dealer == Applier (持有者亲手斩杀) 才回血; 队友/敌人斩杀一律不触发。
          - 不再保留"无受击记录退化给 Applier"的兜底: 规则既已限定"持有者斩杀才回血",
            无法确认斩杀者身份 (如非伤害清除) 时宁可不放回血, 也不让非持有者凭空获益。
          ① 历史条目在真实掉血后才落账, 因此 AfterDeath 触发时致命一击的
             DamageReceivedEntry 必然已在 CombatManager.Instance.History.Entries 中。
          ② 单机下 Applier 即玩家自身, 玩家亲手斩杀时 Dealer == Applier, 行为与直觉一致。 */
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner) return;

        // 斩杀者 = 本 creature 最近一次受击记录 (致命一击) 的 Dealer。
        var killer = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Where(e => e.Receiver == creature)
            .LastOrDefault()?.Dealer;

        // 仅当斩杀者就是标记施加者 (遗物持有者) 本人时才回血; 队友/敌人斩杀不触发。
        if (killer != null && killer == Applier && killer.IsAlive)
        {
            await CreatureCmd.Heal(killer, 3m);
        }
    }
}
