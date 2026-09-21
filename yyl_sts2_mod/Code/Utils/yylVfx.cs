using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     原版特效场景统一播放入口 (照搬 VfxCmd, 2026-09-21)。
///     <para>
///         机制 (反汇编 VfxCmd.PlayVfx): 路径 "vfx/xxx" 经 SceneHelper.GetScenePath
///         解析为 "res://scenes/vfx/xxx.tscn", 再从 PreloadManager.Cache 取场景实例化,
///         挂到目标角色的 VfxContainer / 战斗半场 / 全屏层。
///         <b>原版场景键已预注册, 直接传原版路径是安全的</b>; 若要播 mod 自建场景,
///         Cache 未注册会抛 KeyNotFoundException —— 必须先注册或改走
///         ResourceLoader.Load (姿态 VFX 冻结事故的同款雷)。
///         本封装所有调用包 try/catch: 特效失败只记日志, 绝不让结算中断。
///     </para>
/// </summary>
public static class yylVfx
{
    /// <summary>在单个角色身上播特效 (原版 "vfx/..." 路径, 不带 .tscn)。</summary>
    public static void OnCreature(Creature target, string path)
    {
        try
        {
            if (target == null) return;
            VfxCmd.PlayOnCreature(target, path);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.OnCreature({path}): {ex.Message}");
        }
    }

    /// <summary>在一组角色身上播特效。</summary>
    public static void OnCreatures(IEnumerable<Creature> targets, string path)
    {
        try
        {
            if (targets == null) return;
            VfxCmd.PlayOnCreatures(targets, path);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.OnCreatures({path}): {ex.Message}");
        }
    }

    /// <summary>全屏特效 (华丽收场同款管线)。</summary>
    public static void FullScreen(string path, Creature spawner)
    {
        try
        {
            VfxCmd.PlayFullScreenInCombat(path, spawner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.FullScreen({path}): {ex.Message}");
        }
    }

    /// <summary>
    ///     ★怪物特效挪用: 从 <paramref name="spawner" /> 的角色视觉中心发射同族祭司的
    ///     灵魂光束 (kin_priest_beam, 2026-09-21)。
    ///     <para>
    ///         挂载: 直接 AddChild 到角色的 <see cref="AnimatedSprite2D" /> —— 精灵
    ///         纹理以节点原点居中, 子节点 (0,0) 天然就是角色视觉中心 (我们的角色
    ///         立绘画在容器正 Y 轴以上, 挂容器原点会落在脚心); 找不到精灵时退回
    ///         creatureNode.Visuals。原版是挂场景内预设挂点 "Visuals/Beam", 同理。
    ///     </para>
    ///     <para>
    ///         方向: 光束贴图朝局部 -X 延伸 (原版怪物朝玩家打)。
    ///         <paramref name="flipX" /> = true 时根节点 Scale.X 取 -1 左右镜像 ——
    ///         光束改朝 +X (玩家打右侧敌人), 且 Fire() 内部 tween 只动子节点,
    ///         不会被镜像覆盖。⚠镜像与 180° 旋转不等价 (会把上下也翻)。
    ///     </para>
    ///     <para>
    ///         <c>_Ready</c> 无外部依赖可自由实例化; 播放靠 <c>Fire()</c> (旋转摆动 +
    ///         scale.x 伸出/收回 tween), 完成后仅 Visible=false 不回收 —— 原版复用
    ///         单实例, 我们每次新建, 必须在 <paramref name="lifeSeconds" /> 后 QueueFree。
    ///     </para>
    /// </summary>
    public static void KinBeam(Creature spawner, bool flipX = false, float lifeSeconds = 2.5f, Vector2? positionOffset = null)
    {
        try
        {
            var beam = ResourceLoader
                .Load<PackedScene>("res://scenes/vfx/monsters/kin_priest_beam_vfx.tscn")
                ?.Instantiate<NKinPriestBeamVfx>();
            if (beam == null)
            {
                MainFile.Logger.Error("yylVfx.KinBeam: scene load or instantiate failed");
                return;
            }
            // 优先挂角色精灵 (原点=纹理中心=角色视觉中线); 找不到退回 Visuals 容器。
            Node anchor = yylAnim.FindSprite(spawner)
                ?? (Node)(NCombatRoom.Instance?.GetCreatureNode(spawner)?.Visuals);
            if (anchor == null)
            {
                MainFile.Logger.Error("yylVfx.KinBeam: creature sprite/visuals not found");
                beam.QueueFree();
                return;
            }
            anchor.AddChild(beam);
            beam.Position = positionOffset ?? Vector2.Zero;
            if (flipX)
                beam.Scale = new Vector2(-1f, 1f); // 左右镜像: 光束朝 +X
            beam.Fire();
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/the_kin_priest/the_kin_priest_soul_beam", 1f);
            RecycleLater(beam, lifeSeconds);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.KinBeam: {ex.Message}");
        }
    }

    /// <summary>
    ///     ★华丽收场的命中冲击 (grand_finale_impact, 2026-09-21): 在
    ///     <paramref name="target" /> 身上播放原版华丽收场打中敌人时的冲击序列。
    ///     <para>
    ///         原版两步 (缺一不可): ①静态 <c>Create(Creature)</c> 实例化 + 按目标
    ///         中心/地面 InitializePositions (内部走 Cache.GetScene, 未预热只打
    ///         WARN 但仍会现场加载); ②<b>AddChildSafely 挂进战斗场景</b> ——
    ///         进树后 _Ready 才会自动 PlaySequence, 不挂载就是无声无息。
    ///         序列自管理, 播完自行退出, 无需回收。
    ///     </para>
    /// </summary>
    public static void GrandFinaleImpact(Creature target)
    {
        try
        {
            if (target == null) return;
            var impact = NGrandFinaleImpactVfx.Create(target);
            if (impact == null)
            {
                MainFile.Logger.Error("yylVfx.GrandFinaleImpact: Create returned null (target node missing?)");
                return;
            }
            var room = NCombatRoom.Instance;
            if (room == null)
            {
                impact.QueueFree();
                return;
            }
            room.AddChild(impact);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.GrandFinaleImpact: {ex.Message}");
        }
    }

    /// <summary>延时回收 (光束 Fire 完毕只是隐藏, 必须自己 QueueFree 防节点堆积)。</summary>
    private static async void RecycleLater(Node node, float seconds)
    {
        try
        {
            var tree = node.GetTree();
            if (tree == null) return;
            await node.ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
            if (GodotObject.IsInstanceValid(node))
                node.QueueFree();
        }
        catch
        {
            // 回收失败不影响任何逻辑。
        }
    }

    /// <summary>
    ///     角色立绘的显示高度 (纹理原始高 × 精灵缩放), 用于"上移 N 体位"类定位。
    ///     取不到时返回 0 (调用方偏移自动退化为 0, 无害)。
    /// </summary>
    private static float DisplayHeight(Creature target)
    {
        try
        {
            var anim = yylAnim.FindSprite(target);
            if (anim?.SpriteFrames == null) return 0f;
            // 优先 idle 立绘帧 (最能代表完整体型)。
            foreach (var animName in new[] { "idle_loop", "attack", "cast" })
            {
                if (!anim.SpriteFrames.HasAnimation(animName)) continue;
                var tex = anim.SpriteFrames.GetFrameTexture(animName, 0);
                if (tex != null) return tex.GetHeight() * Math.Abs(anim.Scale.Y);
            }
        }
        catch { }
        return 0f;
    }

    /// <summary>
    ///     在角色身上播特效, 并向上抬高 <paramref name="raiseFraction" /> 个体位
    ///     (1/3 = 角色立绘下三分之一处, 2026-09-21 B 档特效统一用 1/3)。
    ///     走原版 PlayVfx 管线: 挂目标 VfxContainer, 全局坐标 = 角色节点位置 -
    ///     显示高度 × fraction。
    /// </summary>
    public static void OnCreatureRaised(Creature target, string path, float raiseFraction)
    {
        try
        {
            var node = target?.GetCreatureNode();
            if (target == null || node == null) return;
            var pos = node.GlobalPosition - new Vector2(0, DisplayHeight(target) * raiseFraction);
            VfxCmd.PlayVfx(pos, path, target.GetVfxContainer());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.OnCreatureRaised({path}): {ex.Message}");
        }
    }

    /// <summary>
    ///     ★一次性粒子爆发: 挂到 <paramref name="target" /> 的角色精灵 (纹理中心,
    ///     同坐标系自动跟随), 触发 <c>Emitting</c>, <paramref name="lifeSeconds" /> 后回收。
    ///     适用于根节点为 one_shot GPUParticles2D 且无脚本的场景 —— 例如华丽收场的花瓣
    ///     (grand_finale_petals: emitting=false + one_shot=true, <b>必须手动触发
    ///     Emitting</b>, 走 VfxCmd 只会实例化一片静止的粒子, 什么都看不到)。
    ///     原版华丽收场整套是 NCombatVfxSpawner.PlayingGrandFinale 脚本序列, 花瓣只是其中
    ///     一个子场景, 这里单独借用。<paramref name="raiseFraction" /> 上移 N 个体位。
    /// </summary>
    public static void BurstOneShot(Creature target, string path, float lifeSeconds = 4.5f, float raiseFraction = 0f)
    {
        try
        {
            // 挂角色精灵 (纹理中心) 而非容器原点 (脚底)。
            Node anchor = yylAnim.FindSprite(target)
                ?? (Node)(NCombatRoom.Instance?.GetCreatureNode(target)?.Visuals);
            if (anchor == null)
            {
                MainFile.Logger.Error($"yylVfx.BurstOneShot({path}): creature sprite/visuals not found");
                return;
            }
            var scene = ResourceLoader.Load<PackedScene>("res://scenes/" + path + ".tscn");
            if (scene == null)
            {
                MainFile.Logger.Error($"yylVfx.BurstOneShot({path}): scene load failed");
                return;
            }
            var particles = scene.Instantiate<GpuParticles2D>();
            if (particles == null)
            {
                MainFile.Logger.Error($"yylVfx.BurstOneShot({path}): instantiate returned null");
                return;
            }
            anchor.AddChild(particles);
            // 局部偏移受精灵缩放放大: 纹理高 × fraction 的局部距离 ≈ 显示高度 × fraction。
            var texH = 0f;
            if (anchor is AnimatedSprite2D anim && anim.SpriteFrames != null && anim.SpriteFrames.HasAnimation("idle_loop"))
            {
                var tex = anim.SpriteFrames.GetFrameTexture("idle_loop", 0);
                if (tex != null) texH = tex.GetHeight();
            }
            particles.Position = new Vector2(0, -texH * raiseFraction);
            particles.Emitting = true;
            RecycleLater(particles, lifeSeconds);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.BurstOneShot({path}): {ex.Message}");
        }
    }
}
