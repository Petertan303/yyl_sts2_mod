using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     诛邪: 1 费, 造成 12 → 18 伤害。若目标**因此死亡**, **立即处决**其他所有生命值
///     不高于 {Threshold} 点的敌人 (无视格挡)。
///     <para>
///         [balance 2026-09-19] 斩杀线由"最大生命值 50%"改为**定值**, 斩杀仍走伤害管线。
///     </para>
///     <para>
///         [2026-10-02] 斩杀线变量由 <c>WithCalculatedDamage</c> 改为 <c>WithVar</c>:
///         前者是**伤害变量**(带 <c>ValueProp.Move</c>), 会被「炁」的增伤乘区放大,
///         导致卡面显示的斩杀线随炁量膨胀而与实际判定脱节。
///     </para>
///     <para>
///         [2026-10-03] 双重改版:
///         <list type="bullet">
///             <item>处决方式: "造成等同其最大生命的伤害" → **<c>CreatureCmd.Kill</c> 即死</c>。
///                 Kill 内部走<code>Hook.BeforeDeath → AfterDeath → RemoveAllPowersAfterDeath</code>,
///                 死亡钩子齐全(养龙叠层/ 奶龙标记击杀回血都能正常结算),
///                 且不受格挡与"伤害减免类" power 影响, 也不再出现"血量被压到 1 血却没死"的折扣。</item>
///             <item>处决范围: "只斩生命值最低的<b>一个</b>" → **所有 ≤ {Threshold} 的敌人</b>。
///                 原版数据: 第一幕杂兵血量 Inklet 11-18 / CorpseSlug 25-29 / LeafSlime 32-36
///                 多在 40 以下, 而 Myte 61-69 / Chomper 60-67 / 精英 Boss 更高 ——
///                 单斩一个在这些编队里浪费了其余目标。</item>
///             <item>阈值改为**可升级: 40 → 50**(<c>WithVar("Threshold", 40, 10)</c>,
///                 卡面用 <c>{Threshold:diff()}</c> 才有升级差分高亮);
///                 本体伤害 9→13 提到 <b>12→18</b>, 降低"必须恰好斩杀"的苛刻度。</item>
///             <item>特效修复: 处决演出以前"改完代码就不播"了 —— 因为
///                 <c>CreatureCmd.Kill</c> 会 <c>RemoveCreatureNode</c>,
///                 而 <c>GrandFinaleImpact(creature)</c> 依赖 <c>GetCreatureNode</c>拿坐标,
///                 Kill 之后返回 null 静默失败。改为<b>先抓坐标 → Kill → 用坐标版
///                 <c>GrandFinaleImpactAt</c> 重放</b>。</item>
///         </list>
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class ZhuXie(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public ZhuXie() : this(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        WithDamage(12, 6);
        // ★2026-10-02 修「斩杀线随炁增长」: 原来用 WithCalculatedDamage("Threshold", ...),
        //   它建立的是 **CustomCalculatedDamageVar**(带 ValueProp.Move),
        //   于是被「炁」的 IModifyDamageMultiplicative 乘区乘了进去 ——
        //   卡面显示的斩杀线会随炁量线性膨胀, 与实际判定用的
        //   DynamicVars["Threshold"].IntValue(=定值) 严重不符, 属显示 bug。
        //   改用 WithVar(name, baseVal, upgrade) —— 纯整数变量, 不带 ValueProp,
        //   不进任何伤害乘区, 卡面显示恒为 {Threshold}。
        // ★2026-10-03 阈值改为可升级: 基础 40 → 升级 50(第二个参数是"升级后的增量")。
        //   卡面用 {Threshold:diff()} 才能显示 40→50 的升级差分高亮。
        WithVar("Threshold", ThresholdBase, ThresholdUpgrade);
    }

    /// <summary>处决线(基础): 生命值 ≤ 此值的敌人会被即死(定值, 不随炁变化)。</summary>
    public const int ThresholdBase = 40;

    /// <summary>升级带来的处决线增量: 40 → 50。</summary>
    public const int ThresholdUpgrade = 10;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;

        await CommonActions.CardAttack(this, cardPlay, target, DynamicVars.Damage.IntValue, ValueProp.Move)
            .WithHitFx("vfx/vfx_dramatic_stab")
            .Execute(choiceContext);

        // 目标被斩杀后, 处决其他所有达线 (≤ {Threshold} 点) 的敌人。
        if (!target.IsDead) return;

        // ★★ 关键: 必须在 Kill **之前**抓好坐标。
        //   CreatureCmd.Kill → RemoveCreatureNode 会把 NCreature 节点移除, 之后
        //   NGrandFinaleImpactVfx.Create(creature) 里GetCreatureNode 返回 null ⇒ 特效不播。
        //   (这正是"以前有特效、改代码后没特效"的原因 —— 换成了先Kill 再 Create。)
        //   这里先存 (中心点, 脚底点) 两个坐标, Kill 后用坐标版 Create 重放。
        var room = NCombatRoom.Instance;
        var doomed = new List<(Creature Enemy, Vector2 Center, Vector2 Ground)>();
        var threshold = DynamicVars["Threshold"].IntValue;
        foreach (var enemy in Owner.Creature.CombatState?.HittableEnemies
                     ?? Enumerable.Empty<Creature>())
        {
            if (enemy == null || enemy == target || !enemy.IsHittable) continue;
            if (enemy.CurrentHp > threshold) continue;
            var node = room?.GetCreatureNode(enemy);
            if (node == null) continue;   // 拿不到节点就无法定位特效, 跳过(不误伤)
            doomed.Add((enemy, node.VfxSpawnPosition, node.GetBottomOfHitbox()));
        }

        // 主目标的斩杀演出 (此时节点还在, 走带 creature 的重载)。
        yylVfx.GrandFinaleImpact(target);

        foreach (var (enemy, center, ground) in doomed)
        {
            // ★即死: CreatureCmd.Kill 内部走完整死亡钩子(BeforeDeath/AfterDeath/
            //   RemoveAllPowersAfterDeath), 无视格挡与减伤 —— 旧的
            //   "CreatureCmd.Damage(CurrentHp, Unblockable)" 会被"伤害减免"类 power
            //   打折而剩下 1 血, 也吃炁增伤乘区, 语义都不对。
            await CreatureCmd.Kill(enemy);
            // 节点已被移除 ⇒ 必须用**坐标版** Create 才能继续播冲击特效。
            yylVfx.GrandFinaleImpactAt(center, ground);
        }
    }
}
