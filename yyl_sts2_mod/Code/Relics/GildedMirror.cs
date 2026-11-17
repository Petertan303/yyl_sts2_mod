using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Relics;

/// <summary>
///     鎏金镜（罕见）：每当你获得[gold]金光护体[/gold]层数，
///     获得等同于<b>当前持有</b>金光护体层数的格挡。
///     <para>
///         效果在「获得之后」结算（用户定调 2026-11-07）：读的是加完层数后的总量，
///         而不是本次的增量。例如原有 2 层、打出金光符 +2 变成 4 层 → 获得 4 点格挡。
///     </para>
///     <para>
///         ★ 为什么用 <c>AfterPowerAmountChanged</c> 而不是 <c>AfterApplied</c>：
///         引擎 XML 文档明写后者"Does not run if the owner already has this power,
///         and its amount was just changed" —— 只在首次施加时触发，叠层不进钩子
///         （工程里 <c>AegisGainTracker</c> 的注释亦有同一结论）。而本钩子文档写明
///         "Runs <b>after</b> a power's amount is changed, whether it's a new power being
///         added or an existing power's amount being changed"，参数 <c>amount</c> 是
///         "Amount of the power that was added"（本次增量，消耗时为负）。
///         金光在库里有多个施加入口（金光符 / 引光 / 天祭 / 炼化 / 金光壁…），
///         用这一个战斗钩子即可全覆盖，不用逐个改卡牌。
///     </para>
///     <para>
///         ★ 联机：<c>IterateCombatHookListeners</c> 会遍历整场战斗所有监听者，
///         所以必须判 <c>power.Owner == Owner.Creature</c>，否则队友获得金光也会给你格挡。
///         格挡走 <c>CreatureCmd.GainBlock</c> 命令管线（与「温养」同款），各端一致，不用随机数。
///     </para>
///     <para>
///         ★ <c>ValueProp.Unpowered</c>：格挡量严格等于层数，不受力量/姿态等加成放大。
///         钩子整体 try/catch —— 此处抛异常会冻结整场战斗。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modRelicPool))]
public sealed class GildedMirror : yylRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        try
        {
            // 只认「自己身上的金光层数增加」。消耗/掉层时 amount 为负，不触发。
            if (power is not GoldenAegis || amount <= 0m) return;
            if (power.Owner != Owner.Creature) return;

            // 钩子在层数变化之后触发，power.Amount 已是「获得之后」的持有量。
            var stacks = power.Amount;
            if (stacks <= 0m) return;

            await CreatureCmd.GainBlock(Owner.Creature, stacks, ValueProp.Unpowered, null);
            Flash(); // 纯视觉，放最后：即使闪光失败也不影响格挡结算
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[yyl_sts2_mod] 鎏金镜触发失败: {ex.Message}");
        }
    }
}
