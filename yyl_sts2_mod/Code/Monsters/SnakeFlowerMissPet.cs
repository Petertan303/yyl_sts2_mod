using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

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

    public override bool IsHealthBarVisible => true;

    /// <summary>
    ///     ★占位贴图: 直接套用原版奥斯提的视觉场景 (与亡灵契约师的召唤物同一份美术)。
    ///     实测教训 (2026-09-29): 指向伊林的角色 tscn 会 InvalidCastException
    ///     (场景根节点不是宠物所需的视觉类型) → 回退成 ERROR 占位符、没有血条。
    ///     TODO: 美术出图后换成自己的 creature_visuals 场景。
    /// </summary>
    protected override string VisualsPath => "res://scenes/creature_visuals/osty.tscn";

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 空行为: 蛇花小姐不主动出手。
        var idle = new MoveState("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new MonsterState[] { idle }, idle);
    }
}
