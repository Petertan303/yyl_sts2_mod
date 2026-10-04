using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

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

    /// <summary>单道光束: 挂角色精灵、可带起始位置偏移 (供齐射排布)。</summary>
    private static void SpawnBeam(Creature spawner, Vector2 position, bool flipX, float lifeSeconds, bool playSfx)
    {
        try
        {
            var beam = ResourceLoader
                .Load<PackedScene>("res://scenes/vfx/monsters/kin_priest_beam_vfx.tscn")
                ?.Instantiate<NKinPriestBeamVfx>();
            if (beam == null)
            {
                MainFile.Logger.Error("yylVfx.SpawnBeam: scene load or instantiate failed");
                return;
            }
            // 优先挂角色精灵 (原点=纹理中心=角色视觉中线); 找不到退回 Visuals 容器。
            Node anchor = yylAnim.FindSprite(spawner)
                ?? (Node)(NCombatRoom.Instance?.GetCreatureNode(spawner)?.Visuals);
            if (anchor == null)
            {
                MainFile.Logger.Error("yylVfx.SpawnBeam: creature sprite/visuals not found");
                beam.QueueFree();
                return;
            }
            anchor.AddChild(beam);
            beam.Position = position;
            if (flipX)
                beam.Scale = new Vector2(-1f, 1f); // 左右镜像: 光束朝 +X
            beam.Fire();
            if (playSfx)
                SfxCmd.Play("event:/sfx/enemy/enemy_attacks/the_kin_priest/the_kin_priest_soul_beam", 1f);
            RecycleLater(beam, lifeSeconds);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.SpawnBeam: {ex.Message}");
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
        => SpawnBeam(spawner, positionOffset ?? Vector2.Zero, flipX, lifeSeconds, playSfx: true);

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

    /// <summary>
    ///     ★坐标版冲击特效 (2026-10-03): 用于**目标节点已被移除之后**补播华丽收场冲击。
    ///     <para>
    ///         为什么需要: <see cref="GrandFinaleImpact(Creature)" /> 走
    ///         <c>NGrandFinaleImpactVfx.Create(creature)</c>, 内部
    ///         <c>GetCreatureNode(creature)</c> 在 <c>CreatureCmd.Kill</c> 之后会返回
    ///         <c>null</c>(Kill 会 RemoveCreatureNode) ⇒ <b>特效静默不播</b>。
    ///         所以处决类效果必须**先抓坐标 → Kill → 再用本方法按坐标重放**。
    ///     </para>
    /// </summary>
    public static void GrandFinaleImpactAt(Vector2 targetCenterPosition, Vector2 targetGroundPosition)
    {
        try
        {
            var impact = NGrandFinaleImpactVfx.Create(targetCenterPosition, targetGroundPosition);
            if (impact == null)
            {
                MainFile.Logger.Error("yylVfx.GrandFinaleImpactAt: Create returned null");
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
            MainFile.Logger.Error($"yylVfx.GrandFinaleImpactAt: {ex.Message}");
        }
    }

    /// <summary>
    ///     ★光束齐射: <paramref name="count" /> 道灵魂光束的<b>发射起点</b>在身前排成
    ///     一段圆弧 (凸向右, 上下展开呈扇形), 同帧全部发射; 光束本身保持水平不旋转
    ///     (2026-09-21, 白长虫五连射演出)。整体再右移 <paramref name="widthFraction" />
    ///     个角色横向宽度。弧心 = 角色精灵中心, 半径 = 显示宽 × 0.5。
    /// </summary>
    public static void KinBeamColumn(Creature spawner, int count = 5, bool flipX = true, float lifeSeconds = 2.5f,
        float arcDegrees = 50f, float widthFraction = 0.25f)
    {
        var size = DisplaySize(spawner);
        var anim = yylAnim.FindSprite(spawner);
        var sx = Math.Max(anim?.Scale.X ?? 1f, 0.01f);
        var sy = Math.Max(anim?.Scale.Y ?? 1f, 0.01f);
        var radius = size.X * 0.5f;
        var halfArc = arcDegrees / 2f;
        for (var i = 0; i < count; i++)
        {
            var t = count <= 1 ? 0.5f : (float)i / (count - 1); // 0..1, 自上而下
            var ang = (-halfArc + arcDegrees * t) * MathF.PI / 180f;
            // 显示系弧上点 (相对精灵中心): 中间条最靠右 (凸向敌阵), 上下条略靠后。
            var dx = MathF.Cos(ang) * radius + size.X * widthFraction;
            var dy = MathF.Sin(ang) * radius;
            // 换算成精灵局部坐标 (抵消精灵缩放)。
            SpawnBeam(spawner, new Vector2(dx / sx, dy / sy), flipX, lifeSeconds, playSfx: i == 0);
        }
    }

    /// <summary>
    ///     ★弧形/纵向齐射 (通用版, 2026-09-21): <paramref name="count" /> 个自播特效场景
    ///     (NVfxSpine / NVfxParticleSystem 等 _Ready 自动播放的横向特效) 沿目标<b>命中盒
    ///     的垂直分布</b>排列 —— 用 <c>GetTopOfHitbox/GetBottomOfHitbox</c> 拿到头顶与脚底
    ///     的全局坐标后线性插值, <paramref name="spreadFraction" /> = 1 时正好是
    ///     头顶 / 中段 / 脚尖。
    ///     <para>
    ///         ★为什么不用精灵坐标: 敌人遗物/怪物节点里不一定有 AnimatedSprite2D
    ///         (FindSprite 返回 null → 退回 Visuals 容器原点=脚底, 且 DisplaySize 为 0
    ///          → 全部叠在脚底), 这正是"从脚底发出、没有扇形"的根因。命中盒的
    ///         顶/底部是原版自己定位特效时用的同款数据 (参见 NGrandFinaleImpactVfx
    ///         的 InitializePositions), 对玩家和敌人都成立。
    ///     </para>
    ///     <para>
    ///         ★挂载语义: <paramref name="target" /> 既可以是<b>施法者</b> (发射类特效,
    ///         如光束) 也可以是<b>受击者</b> (落点类特效, 如飞斩飞刀) —— 由调用方决定。
    ///         挂载点为 <c>NCombatRoom.Instance</c> 并显式设置 GlobalPosition (与
    ///         <see cref="GrandFinaleImpact" /> 的源头一致), 与粒子/精灵缩放无关。
    ///     </para>
    ///     <paramref name="intervalSeconds" /> &gt; 0 时改为<b>依次落刀</b>: 第 i 个延后
    ///     i × interval 秒才入树 (入树 = _Ready = 开始播放), 形成连斩节奏。
    /// </summary>
    public static void ArcVolley(Creature target, string path, int count, bool flipX = false,
        float lifeSeconds = 3.5f, float spreadFraction = 1f, float intervalSeconds = 0f,
        float xOffsetFraction = 0f)
    {
        try
        {
            if (target == null) return;
            var room = NCombatRoom.Instance;
            var creatureNode = room?.GetCreatureNode(target);
            if (room == null || creatureNode == null)
            {
                MainFile.Logger.Error($"yylVfx.ArcVolley({path}): combat room / creature node missing");
                return;
            }
            var scene = ResourceLoader.Load<PackedScene>("res://scenes/" + path + ".tscn");
            if (scene == null)
            {
                MainFile.Logger.Error($"yylVfx.ArcVolley({path}): scene load failed");
                return;
            }
            // 命中盒顶部=头顶, 底部=脚底 (全局坐标)。两者 x 通常相同。
            var top = creatureNode.GetTopOfHitbox();
            var bottom = creatureNode.GetBottomOfHitbox();
            var height = Math.Abs(bottom.Y - top.Y);
            for (var i = 0; i < count; i++)
            {
                var t = count <= 1 ? 0.5f : (float)i / (count - 1); // 0 = 头顶, 1 = 脚底
                var frac = 0.5f + (t - 0.5f) * spreadFraction; // 以中段为中心上下张开
                var pos = new Vector2(top.X + height * xOffsetFraction,
                    Godot.Mathf.Lerp(top.Y, bottom.Y, frac));
                var node = scene.Instantiate<Node2D>();
                if (node == null) continue;
                AttachLater(room, node, i * intervalSeconds, pos, flipX, lifeSeconds, setAsGlobal: true);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.ArcVolley({path}): {ex.Message}");
        }
    }

    /// <summary>
    ///     延时入树 (2026-09-21 连斩/依次落刀用): 自播场景是<b>入树即播</b>, 所以只要
    ///     延后 AddChild 就能拉出时间间隔。注意 <paramref name="delaySeconds" /> 为 0
    ///     时同步完成挂载 (与旧的同帧齐射行为一致)。目标/节点中途失效则丢弃该次演出。
    ///     <paramref name="setAsGlobal" /> = true 时 <paramref name="position" /> 按全局坐标
    ///     解释 (须在 AddChild 之后赋值, 由父节点变换反算局部坐标)。
    /// </summary>
    private static async void AttachLater(Node parent, Node2D node, float delaySeconds, Vector2 position,
        bool flipX, float lifeSeconds, bool setAsGlobal = false)
    {
        try
        {
            if (delaySeconds > 0f)
            {
                // 节点尚未入树, 用 parent 所在的 SceneTree 计时。
                var tree = parent.GetTree();
                if (tree == null) return;
                await parent.ToSignal(tree.CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);
            }
            if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(node))
            {
                if (GodotObject.IsInstanceValid(node))
                    node.QueueFree();
                return;
            }
            parent.AddChild(node);
            if (setAsGlobal)
                node.GlobalPosition = position;
            else
                node.Position = position;
            if (flipX)
                node.Scale = new Vector2(-1f, 1f);
            RecycleLater(node, lifeSeconds);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.AttachLater: {ex.Message}");
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
    ///     ★聚光灯 (2026-09-30, 天火蓄力; 三轮修订): 从华丽收场场景中<b>单独摘出</b>聚光灯容器,
    ///     照原版 NGrandFinaleVfx.PlaySequence 的定位 —— <b>挂到战斗特效层、移到视口顶部中央</b>
    ///     (不是跟敌人走; 挂敌人身上会因场景内偏移×精灵缩放而错位出黑边)。
    ///     复刻 1s 淡入 + 触发聚光粒子, <paramref name="lifeSeconds" /> 后 0.4s 淡出并回收。
    ///     <para>
    ///         与"实例化整套 NGrandFinaleVfx 再掐断"不同 (那只能卡在 1.2~1.45s,
    ///         晚了会漏花瓣/斩击), 本方法直接摘 <c>spotlight_container</c> 子树,
    ///         <b>任意时长都干净</b> —— 花瓣/斩击/结尾容器根本不会被加入场景树。
    ///         另按实测反馈移除容器内的 <c>vfx_grand_finale_petals_slow</c> (慢花瓣,
    ///         花瓣另有其主), 只留 <c>vfx_common_specks</c> 光尘。
    ///         根的 _Ready 会自动播完整序列, 所以<b>绝不把根加入场景树</b> ——
    ///         实例化后立即摘子节点、丢根。
    ///     </para>
    /// </summary>
    public static void Spotlight(float lifeSeconds)
    {
        try
        {
            var combatVfxContainer = NCombatRoom.Instance?.CombatVfxContainer;
            if (combatVfxContainer == null) return;

            var scene = ResourceLoader.Load<PackedScene>("res://scenes/vfx/vfx_grand_finale.tscn");
            if (scene == null)
            {
                MainFile.Logger.Error("yylVfx.Spotlight: scene load failed");
                return;
            }

            // 实例化根但不入树 (入树即触发脚本 _Ready → 整套演出)。
            var root = scene.Instantiate<Node2D>();
            if (root == null) return;

            // 摘出聚光灯容器 (它自带 NParticlesContainer 脚本), 丢弃其余部分。
            var spotlightContainer = root.GetNode<Node2D>("spotlight_container");
            root.RemoveChild(spotlightContainer);
            root.QueueFree();
            if (spotlightContainer == null) return;

            // 移除慢花瓣 (花瓣特效已被风后奇门占用且用户不要), 只留光尘。
            spotlightContainer.GetNodeOrNull<Node2D>("vfx_grand_finale_petals_slow")?.QueueFree();

            combatVfxContainer.AddChildSafely(spotlightContainer);

            // ★照 PlaySequence: 聚光组件定到视口顶部中央 (原版同款, 光柱自上而下)。
            spotlightContainer.GlobalPosition = new Vector2(spotlightContainer.GetViewportRect().Size.X / 2f, 0f);

            // ★照 Initialize + PlaySequence: spotlight 精灵初始全透明, 1s 淡入到白。
            var spotSprite = spotlightContainer.GetNode<Node2D>("spotlight");
            if (spotSprite != null)
            {
                spotSprite.Modulate = new Color(1f, 1f, 1f, 0f);
                var fadeIn = spotlightContainer.CreateTween();
                fadeIn.TweenProperty(spotSprite, "modulate", Colors.White, 1.0);
            }

            // 触发聚光粒子 (_spotlightParticles 就是容器自身, Restart() 公开)。
            if (spotlightContainer is NParticlesContainer particlesContainer)
            {
                particlesContainer.Restart();
            }
            else
            {
                // 兜底: 直接点亮容器内所有粒子。
                foreach (var p in spotlightContainer.GetChildren().OfType<GpuParticles2D>())
                {
                    p.Emitting = true;
                }
            }

            FadeOutLater(spotlightContainer, lifeSeconds);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.Spotlight: {ex.Message}");
        }
    }

    /// <summary>延时 0.4s 淡出后回收 (聚光灯退场, 避免硬消失)。</summary>
    private static async void FadeOutLater(Node node, float lifeSeconds)
    {
        try
        {
            var tree = node.GetTree();
            if (tree == null) return;
            var wait = MathF.Max(lifeSeconds - 0.4f, 0f);
            await node.ToSignal(tree.CreateTimer(wait), SceneTreeTimer.SignalName.Timeout);
            if (!GodotObject.IsInstanceValid(node)) return;

            var fade = node.CreateTween();
            fade.TweenProperty(node, "modulate", new Color(1f, 1f, 1f, 0f), 0.4);
            await node.ToSignal(tree.CreateTimer(0.45), SceneTreeTimer.SignalName.Timeout);
            if (GodotObject.IsInstanceValid(node))
            {
                node.QueueFree();
            }
        }
        catch
        {
            // 淡出失败不影响任何逻辑。
        }
    }

    /// <summary>
    ///     ★呼唤波纹 (2026-09-30, 天火蓄力): 复刻原版灵魂异鱼「呼唤」(Beckon) 的波纹特效。
    ///     <para>
    ///         原理 (反编译 NSoulFyshVfx): 波纹不是独立粒子场景, 而是鱼骨架 Spine 附件
    ///         (beckonwave 网格) 套着色器材质 <c>soul_fysh_beckonwave_mat</c> —— 着色器用
    ///         噪声纹理扰动 + <c>amount</c> 参数控制波纹显影 (0.3 隐 → 1.0 全显)。
    ///         StartBeckon = amount 0.3→1.0 (0.25s EaseOut Quad); EndBeckon = 1.0→0.3 (0.5s EaseIn Quad)。
    ///         材质/贴图都在游戏 pck 内, 运行时直接加载; 材质必须 <c>Duplicate()</c>
    ///         (原版共享单实例, 直接改 amount 会互相串扰)。原版形状挂在鱼身上,
    ///         我们改为独立 Sprite2D 挂施法者精灵中心, 宽度按施法者显示高度定标。
    ///     </para>
    ///     <para>
    ///         时间线: 快速淡入 + amount 展开 0.45s → 保持蓄力 → 结束前 0.5s amount 收回 +
    ///         0.4s 淡出 → 回收。整体随生命周期轻微向外扩张 (波纹感)。
    ///     </para>
    /// </summary>
    public static void BeckonRipple(Creature caster, float lifeSeconds = 2.0f, float sizeFactor = 1.5f)
    {
        try
        {
            if (caster == null) return;
            Node anchor = yylAnim.FindSprite(caster)
                ?? (Node)(NCombatRoom.Instance?.GetCreatureNode(caster)?.Visuals);
            if (anchor == null) return;

            var material = ResourceLoader
                .Load<ShaderMaterial>("res://materials/vfx/monsters/soul_fysh_beckonwave_mat.tres")
                ?.Duplicate() as ShaderMaterial;
            var texture = ResourceLoader.Load<Texture2D>("res://vfx/monsters/soul_fysh/beckonwave.png");
            if (material == null || texture == null)
            {
                MainFile.Logger.Error("yylVfx.BeckonRipple: material/texture load failed");
                return;
            }

            var wave = new Sprite2D
            {
                Texture = texture,
                Material = material,
                // 贴图原始分辨率是 Spine 大画布, 按施法者显示高度定标。
                Scale = Vector2.One * (DisplayHeight(caster) * sizeFactor / MathF.Max(texture.GetWidth(), 1f)),
                Modulate = new Color(1f, 1f, 1f, 0f),
            };
            anchor.AddChild(wave);

            var startScale = wave.Scale;
            var peak = material; // 便于阅读
            const float restAmount = 0.3f;

            // 展开: amount 0.3→1.0 (原版 StartBeckon 0.25s, 稍放慢到 0.45s) + 轻微外扩 + 淡入。
            var rise = wave.CreateTween();
            rise.SetParallel(true);
            rise.TweenProperty(peak, "shader_parameter/amount", 1f, 0.45)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out).From(restAmount);
            rise.TweenProperty(wave, "modulate", Colors.White, 0.3).From(new Color(1f, 1f, 1f, 0f));
            rise.TweenProperty(wave, "scale", startScale * 1.15f, lifeSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);

            // 收回: 结束前 0.5s amount 回落 (原版 EndBeckon) + 淡出, 然后回收。
            RecycleWave(wave, peak, lifeSeconds, restAmount);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.BeckonRipple: {ex.Message}");
        }
    }

    /// <summary>呼唤波纹的收尾: 结束前 0.5s amount 回落 + 0.4s 淡出, 随后回收。</summary>
    private static async void RecycleWave(Node wave, ShaderMaterial material, float lifeSeconds, float restAmount)
    {
        try
        {
            var tree = wave.GetTree();
            if (tree == null) return;
            var wait = MathF.Max(lifeSeconds - 0.5f, 0f);
            await wave.ToSignal(tree.CreateTimer(wait), SceneTreeTimer.SignalName.Timeout);
            if (!GodotObject.IsInstanceValid(wave)) return;

            var fall = wave.CreateTween();
            fall.SetParallel(true);
            fall.TweenProperty(material, "shader_parameter/amount", restAmount, 0.5)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            fall.TweenProperty(wave, "modulate", new Color(1f, 1f, 1f, 0f), 0.4);
            await wave.ToSignal(tree.CreateTimer(0.55), SceneTreeTimer.SignalName.Timeout);
            if (GodotObject.IsInstanceValid(wave))
                wave.QueueFree();
        }
        catch
        {
            // 收尾失败不影响任何逻辑。
        }
    }

    /// <summary>角色立绘的显示尺寸 (纹理原始宽高 × 精灵缩放), 用于"上移 N 体位"类定位。</summary>
    internal static Vector2 DisplaySize(Creature target)
    {
        try
        {
            var anim = yylAnim.FindSprite(target);
            if (anim?.SpriteFrames == null) return Vector2.Zero;
            // 优先 idle 立绘帧 (最能代表完整体型)。
            foreach (var animName in new[] { "idle_loop", "attack", "cast" })
            {
                if (!anim.SpriteFrames.HasAnimation(animName)) continue;
                var tex = anim.SpriteFrames.GetFrameTexture(animName, 0);
                if (tex != null) return new Vector2(tex.GetWidth(), tex.GetHeight()) * anim.Scale;
            }
        }
        catch { }
        return Vector2.Zero;
    }

    /// <summary>角色立绘的显示高度。</summary>
    internal static float DisplayHeight(Creature target) => DisplaySize(target).Y;

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
