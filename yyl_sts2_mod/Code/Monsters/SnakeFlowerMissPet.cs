using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace yyl_sts2_mod.Code.Monsters;

/// <summary>
///     宠物「蛇花小姐」—— 由遗物 <see cref="Relics.SnakeFlowerMiss" /> 在战斗开始时召唤。
///     <para>
///         照原版宠物范本 <c>Byrdpip</c> 写: 定死初始生命、隐藏血条由调用方决定、
///         行为机是一个什么都不做的空状态 (它不会自己行动, 只会挨打;
///         输出靠卡牌「蛇花咬」替它出手)。
///     </para>
/// </summary>
public sealed class SnakeFlowerMissPet : CustomMonsterModel
{
    /// <summary>基础生命上限 (遗物每存活一场 +3)。</summary>
    public const int BaseHp = 8;

    public override int MinInitialHp => BaseHp;

    public override int MaxInitialHp => BaseHp;

    // v0.113.0: IsHealthBarVisible 已被 MonsterModel.IsInteractable 取代 (true = 可选中且显示血条)。
    public override bool IsInteractable => true;

    /// <summary>
    ///     视觉场景: 本 mod 自带的奥斯提副本 (从 reference_osty 搬入, 2026-10-20)。
    ///     原先指向本体 <c>res://scenes/creature_visuals/osty.tscn</c>, 现改为本地副本 ——
    ///     以后手改形象直接编辑 <c>yyl_sts2_mod/scenes/creature_visuals/</c> 下的文件, 不再影响判断来源。
    ///     实测教训 (2026-09-29): 指向伊林的角色 tscn 会 InvalidCastException
    ///     (场景根节点不是宠物所需的视觉类型) → 回退成 ERROR 占位符、没有血条。
    /// </summary>
    protected override string VisualsPath => "res://yyl_sts2_mod/scenes/creature_visuals/osty.tscn";

    /// <summary>
    ///     ★ BaseLib 钩子: 优先于引擎 <c>MonsterModel.CreateVisuals</c> 被调用
    ///     (<c>BaseLib.Abstracts.CreateVisuals</c> prefix)。返回非 null 则跳过引擎原始加载。
    ///     <para>
    ///         与角色类 (<c>yyl_sts2_mod.CreateCustomVisuals</c>) 同款模式:
    ///         用 <c>NodeFactory&lt;NCreatureVisuals&gt;.CreateFromScene</c> 而不是依赖场景根的脚本。
    ///         原因 (2026-10-20 实测): Godot 的二进制场景导出曾把根节点的引擎 C# 脚本引用剥掉,
    ///         运行时根节点是裸 Node2D, 引擎的 cast 失败 → ERROR 占位符;
    ///         且对已实例化的 Godot 对象 <c>SetScript</c> 无法更换托管类型 (wrapper 已固定为 Node2D),
    ///         手动挂脚本这条路走不通。NodeFactory 会新建真正的 NCreatureVisuals 并把
    ///         场景的属性与整棵子树 (Visuals/Bounds/IntentPos/CenterPos 命名节点) 迁移过去,
    ///         对场景形态变化免疫 —— 这正是 LexNinja2 使用的模式。
    ///     </para>
    /// </summary>
    public override NCreatureVisuals CreateCustomVisuals()
    {
        var visuals = NodeFactory<NCreatureVisuals>.CreateFromScene(VisualsPath);
        if (visuals == null)
            MainFile.Logger.Warn($"[yyl_sts2_mod] 蛇花小姐视觉场景转换失败: {VisualsPath}");
        return visuals;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 空行为: 蛇花小姐不主动出手。
        var idle = new MoveState("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new MonsterState[] { idle }, idle);
    }
}
