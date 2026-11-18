using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using yyl_sts2_mod.Code.Powers;

namespace yyl_sts2_mod.Code.Utils;

/// <summary>
///     大瓶黄桃罐头（<see cref="Relics.NlCan2" />）的"变身"视觉：持有后把伊林立绘换成奶龙。
///     <para>
///         独立场景 <c>scenes/nailong_visuals.tscn</c> 内含两个姿势 Sprite2D（脚底对齐、等高），
///         本控制器把它实例化到角色 visuals 容器下，隐藏原 AnimatedSprite2D(名为 Visuals)。
///         ★有格挡 = 马步推掌(Guard)，无格挡 = 立正抱肚(Idle)，由 <c>Creature.BlockChanged</c> 驱动。
///     </para>
///     <para>
///         幂等：安装状态挂在节点 meta 上；重复调用只刷新姿势。失去遗物时调 <see cref="Remove" /> 恢复。
///         全程 try/catch —— 视觉代码抛异常绝不能冻结战斗。
///     </para>
/// </summary>
public static class NailongVisuals
{
    private const string ScenePath = "res://yyl_sts2_mod/scenes/nailong_visuals.tscn";
    private const string NodeName = "NailongVisuals";

    // 格挡事件委托必须强引用保活, 且反注册时要拿回原委托 —— Godot meta 存不了 C# 委托, 用静态表按 Creature 索引。
    // ★2026-11-18: 用 ConditionalWeakTable —— 玩家对象每场战斗重建, 普通 Dictionary 会永久强引用
    //   死掉的 Creature + 捕获旧节点的委托 (实测泄漏); CWT 的键被 GC 时值自动释放, 无需 AfterCombatEnd 清理。
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Creature, System.Action<int, int>> BlockHandlers = new();

    /// <summary>安装/刷新奶龙视觉。应在角色 visuals 节点可用后调用（战斗开始、房间载入）。</summary>
    public static void Apply(Creature creature)
    {
        try
        {
            if (creature == null || creature.GetCreatureNode() is not NCreature n || n.Visuals == null)
                return;

            var visuals = n.Visuals;

            // 已安装 → 只刷新姿势
            if (visuals.GetNodeOrNull<Node2D>(NodeName) is { } existing)
            {
                RefreshPose(creature, existing);
                return;
            }

            var scene = ResourceLoader.Load<PackedScene>(ScenePath);
            if (scene == null)
            {
                MainFile.Logger.Warn("[yyl] 奶龙视觉: 场景加载失败 " + ScenePath);
                return;
            }
            var inst = scene.Instantiate<Node2D>();
            inst.Name = NodeName;
            visuals.AddChild(inst);

            // 层级：紧跟在原 Visuals 之后（同级树顺序决定前后，不用 z_index）。
            if (visuals.GetNodeOrNull("Visuals") is Node original)
                visuals.MoveChild(inst, original.GetIndex() + 1);

            // 隐藏原逐帧立绘（金光 SDF 会跟随当前帧，隐藏后 SDF 也停在那帧——可接受）
            if (visuals.GetNodeOrNull("Visuals") is CanvasItem orig)
                orig.Visible = false;

            // 订阅格挡变化切姿势（保存委托引用以便反注册 / CWT 自动回收）
            if (!BlockHandlers.TryGetValue(creature, out _))
            {
                System.Action<int, int> handler = (_, _) => RefreshPose(creature, inst);
                creature.BlockChanged += handler;
                BlockHandlers.Add(creature, handler);
            }

            RefreshPose(creature, inst);
            MainFile.Logger.Info("[yyl] 奶龙视觉: 已安装");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 奶龙视觉安装失败: {ex.Message}");
        }
    }

    /// <summary>移除奶龙视觉、恢复原立绘（失去遗物时调用）。</summary>
    public static void Remove(Creature creature)
    {
        try
        {
            if (creature == null || creature.GetCreatureNode() is not NCreature n || n.Visuals == null)
                return;
            var visuals = n.Visuals;
            if (visuals.GetNodeOrNull<Node2D>(NodeName) is not { } inst) return;

            // 反注册格挡事件
            if (BlockHandlers.TryGetValue(creature, out var handler))
            {
                creature.BlockChanged -= handler;
                BlockHandlers.Remove(creature);
            }

            inst.QueueFree();
            if (visuals.GetNodeOrNull("Visuals") is CanvasItem orig)
                orig.Visible = true;
            // 金光轮廓恢复原伊林逐帧 SDF
            GoldenAegis.SetNailongPose(creature, 0);
            MainFile.Logger.Info("[yyl] 奶龙视觉: 已移除");
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 奶龙视觉移除失败: {ex.Message}");
        }
    }

    /// <summary>按当前格挡切换姿势。</summary>
    private static void RefreshPose(Creature creature, Node2D inst)
    {
        try
        {
            var hasBlock = creature.Block > 0;
            if (inst.GetNodeOrNull<Sprite2D>("Guard") is { } guard) guard.Visible = hasBlock;
            if (inst.GetNodeOrNull<Sprite2D>("Idle") is { } idle) idle.Visible = !hasBlock;
            // 金光轮廓同步换成对应姿势的奶龙 SDF (pose: 1=立正, 2=马步)
            GoldenAegis.SetNailongPose(creature, hasBlock ? 2 : 1);
        }
        catch (System.Exception ex)
        {
            MainFile.Logger.Warn($"[yyl] 奶龙姿势刷新失败: {ex.Message}");
        }
    }
}
