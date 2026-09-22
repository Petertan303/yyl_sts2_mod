using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace yyl_sts2_mod.Code.Nodes;

/// <summary>
///     战斗角色视觉节点。继承 BaseLib 的 <see cref="NCreatureVisuals" />,
///     配合 yylCharacter.tscn 里名为 <c>Visuals</c> 的 AnimatedSprite2D (逐帧精灵) 使用。
///     <para>
///         不需要 Spine / AnimationTree / Eye —— 那些是 Watcher 模板的残留, 本 mod 是帧动画,
///         直接交给基类的 <c>%Visuals</c> 节点驱动即可。
///     </para>
/// </summary>
[GlobalClass]
public partial class yylNCreatureVisuals : NCreatureVisuals
{
}
