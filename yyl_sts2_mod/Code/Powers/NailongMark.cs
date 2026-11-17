using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

public sealed class NailongMark : yylPowerModel
{
    public override PowerType Type => PowerType.None; 
    public override PowerStackType StackType => PowerStackType.None;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    
    /*  ★2026-11-15 用户定调: 初始遗物(黄桃罐头)的回血改为「奶龙死亡时即生效」——
        不再要求持有者亲手斩杀, 队友/敌人/环境把带标记的奶龙弄死一样回血。
          - 回血对象仍是 Applier (挂标记的遗物持有者), 与升级版 InnerDemon 一致。
          - 旧的 FEED/狂宴式斩杀者判定 (DamageReceivedEntry.LastOrDefault()?.Dealer)
            已整段删除: 它正是"奶龙死了却不回血"的根源。
          - 非伤害清除等非正常死亡同样不回血 (wasRemovalPrevented 守卫保留)。 */
    // v0.113.0: AbstractModel.AfterDeath 新增第 3 个参数 source (造成死亡的卡)。
    // ★2026-11-17 用户定调: 关闭「奶龙死亡回血」, 整段逻辑注释保留 (日后想恢复直接解开)。
    //   标记本身的 identity 用途 (供养龙/心防/驭龙等卡识别奶龙) 不受影响, 仅取消回血。
    // public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, CardModel? source, bool wasRemovalPrevented, float deathAnimLength)
    // {
    //     if (wasRemovalPrevented || creature != Owner) return;
    //     if (Applier == null || !Applier.IsAlive) return;
    //
    //     await CreatureCmd.Heal(Applier, 3m);
    // }
}
