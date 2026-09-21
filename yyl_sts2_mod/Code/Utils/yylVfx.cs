using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
    ///     ★怪物特效挪用实验: 从 <paramref name="spawner" /> 位置发射同族祭司的灵魂光束
    ///     (kin_priest_beam, 2026-09-21)。反汇编确认:
    ///     <c>_Ready</c> 只缓存节点引用并隐藏自身, 无外部依赖, 可自由实例化;
    ///     播放靠 <c>Fire()</c> (旋转摆动 + 光束 scale.x 伸出/收回的 tween), tween 完成后
    ///     仅 Visible=false <b>不回收节点</b> —— 原版由祭司自己复用同一实例, 我们每次
    ///     新建实例, 因此必须在 <paramref name="lifeSeconds" /> 后 QueueFree。
    ///     光束贴图朝局部 -X 延伸: 默认 0° 向左, 玩家朝右打敌人传 180°。
    /// </summary>
    public static void KinBeam(Creature spawner, float rotationDegrees = 0f, float lifeSeconds = 2.5f)
    {
        try
        {
            var scene = ResourceLoader.Load<PackedScene>("res://scenes/vfx/monsters/kin_priest_beam_vfx.tscn");
            if (scene == null)
            {
                MainFile.Logger.Error("yylVfx.KinBeam: scene load failed");
                return;
            }
            var beam = scene.Instantiate<NKinPriestBeamVfx>();
            if (beam == null)
            {
                MainFile.Logger.Error("yylVfx.KinBeam: instantiate returned null");
                return;
            }
            var container = spawner.GetVfxContainer();
            if (container == null)
            {
                beam.QueueFree();
                return;
            }
            container.AddChild(beam);
            // ★VfxContainer 是全屏层(原点=屏幕左上角), 必须显式定位到施法者
            //   (原版 PlayOnCreature 同款: 取 creature 节点的 GlobalPosition)。
            //   不设置的话光束落在屏幕左上角并朝屏幕外喷, 看不见。
            beam.GlobalPosition = spawner.GetCreatureNode().GlobalPosition;
            beam.RotationDegrees = rotationDegrees;
            beam.Fire();
            RecycleLater(beam, lifeSeconds);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"yylVfx.KinBeam: {ex.Message}");
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
}
