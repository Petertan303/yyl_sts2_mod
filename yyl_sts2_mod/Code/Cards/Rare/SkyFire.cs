using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Abstract;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Commands;
using yyl_sts2_mod.Code.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     天火: 2 费, 失去所有炁, 对所有敌人造成 失去值 × 8 → 12 伤害。
///     [rule 2026-09-18] 炁不足时无额外效果 (不打出一滴伤害)。
///     <para>
///         ⚠ 强清场: 10 炁时群伤 80 → 120; 但炁伤害乘区已改对数,
///         大量囤炁的直接收益主要就体现在这张卡上。
///     </para>
///     <para>
///         ★2026-09-30 演出重做: 照华丽收场 GrandFinale 的「前摇→伤害」骨架 + 重锤 Bludgeon 的
///         落击写法。炁量经滚石 RollingBoulder 的渐近饱和曲线 (ratio = qi/(qi+40)) 同时驱动
///         ①特效尺寸 (1.0→1.8) 与 ②前摇时长 (0.35→1.25s) —— 炁越大, 天火酝酿越久、砸得越猛。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class SkyFire(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    /// <summary>滚石同款饱和分母: ratio = qi / (qi + 40), 40 炁时已到 50% 缩放, 永不超 1。</summary>
    public const decimal SaturationDivisor = 40m;

    /// <summary>特效基础尺寸 (0 炁兜底, 实际 0 炁打不出)。</summary>
    public const float MinScale = 1.0f;

    /// <summary>缩放振幅: 满ratio时尺寸 = MinScale + ScaleAmp (1.8 倍)。</summary>
    public const float ScaleAmp = 0.8f;

    /// <summary>前摇基础秒数 (≥1.3s: 保证聚光灯完整淡入 + 天际光斑亮起)。</summary>
    public const float MinPreDelay = 1.3f;

    /// <summary>前摇振幅: 满ratio时 = MinPreDelay + PreDelayAmp (约 2.2s)。</summary>
    public const float PreDelayAmp = 0.9f;

    /// <summary>落击之后、地表火爆爆开之前的固定间隔 (实测 0.3s 偏长, 改 0.1s)。</summary>
    public const float FireBurstDelay = 0.0f;

    /// <summary>重锤尺寸 = 炁缩放(scale) × 此系数。目的: 让重锤与火爆视觉大小相近
    /// (实测火爆天生偏大、重锤偏小); 采用「随炁×1.5」方案而非「火爆0.75/重锤1.25」。</summary>
    public const float HeavyBluntScale = 1.8f;

    public SkyFire() : this(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithDamage(8, 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        var current = Owner.Creature.GetPower<Qi>()?.Amount ?? 0;
        if (current <= 0) return;

        var totalDamage = DynamicVars.Damage.IntValue * current;

        // ★滚石同款渐近饱和: 炁越大 ratio 越接近 1 但永不超 1, 特效/前摇不会失控。
        var ratio = (float)(current / (current + SaturationDivisor));
        var scale = MinScale + ratio * ScaleAmp;
        var preDelay = MinPreDelay + ratio * PreDelayAmp;
        var enemies = combatState.HittableEnemies.ToList();

        /*  ★四段演出 (2026-09-30, 对话定案; 三轮修订):
            阶段一 聚光灯  : yylVfx.Spotlight —— 从华丽收场场景**单独摘出**聚光灯容器
                             (照风后奇门借花瓣的同思路), 不入树的根被丢弃,
                             花瓣/斩击/结尾容器根本不进场景树 → **前摇任意时长都干净**,
                             不再需要固定 1.2s 掐断。复刻原版 1s 淡入, 结束前 0.4s 淡出。
            阶段二 天降火光: vfx_missile_sky_flare —— 原版"大型魔法飞弹"测试场的
                             蓄势天际光斑 (真场景), 借 BurstOneShot 管线手动触发
                             Emitting 并抬到头顶之上, 撑住聚光灯淡出后的蓄势时间。
            阶段三 落击    : 重锤特效(放大) + 伤害同步。
            阶段四 地表火爆: 落击后固定 0.3s, NFireBurstVfx(放大)原地爆开。
            (肾上腺素聚炁特效已按实测反馈移除 —— 风格不合。) */

        // 阶段一: 聚光灯 ×3 层叠加 (单层偏淡, 原版即如此; 实测反馈加多层), 固定屏幕位。
        yylVfx.Spotlight(preDelay + 0.5f);
        yylVfx.Spotlight(preDelay + 0.5f);
        yylVfx.Spotlight(preDelay + 0.5f);

        const float rippleAt = 0.4f;
        const float skyFlareAt = 0.9f;

        // 阶段一点五: 呼唤波纹 (灵魂异鱼「呼唤」同款, 2026-09-30) —— 打出后 0.4s 起纹,
        // 与聚光灯/天降火光并行, 围绕施法者蓄力。时长盖住整个前摇, 落击前收回。
        await Cmd.Wait(rippleAt);
        yylVfx.BeckonRipple(Owner.Creature, lifeSeconds: preDelay, sizeFactor: 1.6f);

        await Cmd.Wait(skyFlareAt - rippleAt);

        // 阶段二 (确保落击前至少 0.4s 天际光斑已经亮起)
        if (preDelay > skyFlareAt + 0.4f)
        {
            foreach (var enemy in enemies)
            {
                yylVfx.BurstOneShot(
                    enemy, "vfx/missile/vfx_missile_sky_flare",
                    lifeSeconds: preDelay - skyFlareAt + 1.5f, raiseFraction: 1.4f);
            }
        }

        // 等满整个前摇 (聚光灯由 yylVfx 自管生命周期, 含淡出)。
        if (preDelay > skyFlareAt)
        {
            await Cmd.Wait(preDelay - skyFlareAt);
        }

        // 阶段三: 落击瞬间: 每个敌人同时获得自上而下的重击特效 (手动实例化以放大), 与伤害同步。
        // ★落点 = GetBottomOfHitbox (脚底, NFireBurstVfx 同款) —— VfxSpawnPosition 在
        //   角色半空, 实测重锤会悬在头顶 (2026-09-30 截图反馈)。
        foreach (var enemy in enemies)
        {
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(enemy);
            if (creatureNode != null)
            {
                // 路径型特效没有缩放参数 → 照 VfxCmd.PlayVfx 内部写法手动实例化
                // (GetScenePath→Instantiate→挂该敌人的 vfx 容器→设坐标), 再放大节点。
                var scenePath = SceneHelper.GetScenePath("vfx/vfx_heavy_blunt");
                var blunt = PreloadManager.Cache.GetScene(scenePath)
                    .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
                enemy.GetVfxContainer()?.AddChildSafely(blunt);
                blunt.GlobalPosition = creatureNode.GetBottomOfHitbox();
                blunt.Scale = Vector2.One * (scale * HeavyBluntScale);
            }
        }

        /*  ★2026-09-22 修复: 先结算伤害(炁还在, 乘区与预览同源)再失炁。
            ★2026-09-23 修复: AllEnemies 卡, CommonActions.CardAttack 忽略 target 自动打全体,
            原 foreach 循环会把全体伤害按敌人数翻倍 → 改为单次调用。
            ★2026-09-30: 命中特效已由上面的缩放节点承担, 这里只保留重击音效。 */
        await DamageCmd.Attack(totalDamage).FromCard(this, cardPlay).TargetingAllOpponents(combatState)
            .WithHitFx(null, null, "heavy_attack.mp3")
            .Execute(choiceContext);

        // 伤害打完再清空炁
        await yylCmd.LoseQi(choiceContext, Owner, current, this, cardPlay.Card);

        // 阶段四: 地表火爆 —— 落击后固定 0.3s 原地爆开 (与重锤错开, 二段观感)。
        await Cmd.Wait(FireBurstDelay);
        foreach (var enemy in enemies)
        {
            // NFireBurstVfx.Create 不自动挂父 (TheArchitect 同款用法: 手动 AddChildSafely)。
            // _Ready 里自动 PlaySequence (粒子重启 + 弱震屏, ~2s 后自毁)。
            NCombatRoom.Instance?.CombatVfxContainer
                .AddChildSafely(NFireBurstVfx.Create(enemy, scale));
        }
    }
}
