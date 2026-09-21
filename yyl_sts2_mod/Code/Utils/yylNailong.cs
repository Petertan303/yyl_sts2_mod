using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Utils;

public static class yylNailong
{
    /// <summary>
    ///     只判断"是不是奶龙"(身上带着奶龙标记/心魔), 不要求存活。
    ///     <para>
    ///         击杀钩子 (<c>AfterDeath</c>) 里怪物 <c>IsAlive</c> 已经是 false,
    ///         所以「养龙」遗物计数必须用这个版本, 否则永远不涨层。
    ///     </para>
    /// </summary>
    public static bool IsNailongMarked(Creature? creature) =>
        creature != null &&
        (creature.HasPower<NailongMark>() || creature.HasPower<InnerDemon>());

    public static bool IsNailong(Creature? creature) =>
        creature is { IsAlive: true } && IsNailongMarked(creature);

    public static bool IsNailongSource(Creature? source) =>
        IsNailong(source) || IsNailong(source?.PetOwner?.Creature);

    /// <summary>
    ///     ★施加奶龙标记的**唯一入口** (2026-09-21, 联机去重)。
    ///     <para>
    ///         两个标记都是 <c>InstancedPerApplier</c>: 联机下两名玩家各自的黄桃罐头
    ///         会给同一只怪物挂上两个独立实例, 卡面上出现两个奶龙 buff。
    ///         这里统一在施加前过滤 —— <b>一个目标最多只有一个奶龙标记</b>。
    ///     </para>
    ///     <paramref name="demon" /> = false 时打普通奶龙: 已带<b>任意</b>奶龙标记
    ///     (含心魔) 的目标一律跳过 —— 心魔优先, 不被普通奶龙覆盖;
    ///     = true 时打心魔: 已带心魔的跳过, 已带普通奶龙的<b>先移除再用心魔覆盖</b>。
    /// </summary>
    public static async Task ApplyMark(
        PlayerChoiceContext ctx,
        IEnumerable<Creature> targets,
        Creature applier,
        bool demon = false)
    {
        var list = targets.Where(c => c is { IsAlive: true }).Distinct().ToList();
        if (list.Count == 0) return;

        if (demon)
        {
            // 心魔: 跳过已有心魔的 (联机去重)。
            var need = list.Where(c => !c.HasPower<InnerDemon>()).ToList();
            if (need.Count == 0) return;
            // 已带普通奶龙的: 心魔优先 —— 先移除普通奶龙, 再上心魔。
            foreach (var creature in need)
            foreach (var mark in creature.GetPowerInstances<NailongMark>().ToList())
                await PowerCmd.Remove(mark);
            await PowerCmd.Apply<InnerDemon>(ctx, need, 1m, applier, null);
            return;
        }

        // 普通奶龙: 已带任意奶龙标记的目标跳过 (心魔不会被覆盖)。
        var fresh = list.Where(c => !IsNailongMarked(c)).ToList();
        if (fresh.Count == 0) return;
        await PowerCmd.Apply<NailongMark>(ctx, fresh, 1m, applier, null);
    }
}
