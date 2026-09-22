using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using yyl_sts2_mod.Code.Stances;

namespace yyl_sts2_mod.Code.UI;

/// <summary>
/// 独立于能力栏的「姿态指示」UI，按用户设计要求拆成两部分：
/// <list type="bullet">
///   <item><description><b>可见 logo</b>：角色左下角的一枚小图标，纯展示、放大、不加文字，不遮挡战斗画面。</description></item>
///   <item><description><b>透明命中区</b>：覆盖角色身体的一块透明 <see cref="Control"/>，鼠标移到角色身上即显示悬浮框
///   （与 power 图标的悬浮行为一致），让玩家自然地把鼠标移到角色上查看当前姿态效果。</description></item>
/// </list>
/// 两者都接 <c>MouseEntered</c> 信号后调用角色节点 <c>NCreature.ShowHoverTips(IEnumerable&lt;IHoverTip&gt;)</c>，
/// 把姿态的 <see cref="yylStanceModel.DumbHoverTip" /> 转交引擎渲染。整段包 try/catch，UI 异常绝不阻断战斗。
/// </summary>
public sealed partial class StanceTagIndicator : Control
{
    private Player? _owner;
    private yylStanceModel? _stance;
    private TextureRect? _logo;
    private Control? _hitArea;

    // 角色大致包围盒（逻辑 80×63 ×3 ≈ 240×189）。以 creatureNode.GlobalPosition（脚底中心）为原点向上铺。
    private const float BodyWidth = 150f;
    private const float BodyHeight = 190f;
    private const float LogoSize = 56f;
    private const float LogoGap = 6f;

    public StanceTagIndicator()
    {
        MouseFilter = MouseFilterEnum.Ignore; // 容器本身不拦截，由子节点各自处理

        // 透明命中区：覆盖角色身体，鼠标移入即显示悬浮框（仿 power 图标的悬浮行为）
        _hitArea = new Control
        {
            Name = "StanceHitArea",
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(BodyWidth, BodyHeight),
        };
        AddChild(_hitArea);
        _hitArea.Connect(Control.SignalName.MouseEntered, Callable.From(OnHovered));
        _hitArea.Connect(Control.SignalName.MouseExited, Callable.From(OnUnhovered));

        // 可见 logo：角色左下角，纯展示、放大、不加文字
        _logo = new TextureRect
        {
            Name = "StanceLogo",
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(LogoSize, LogoSize),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        AddChild(_logo);
        _logo.Connect(Control.SignalName.MouseEntered, Callable.From(OnHovered));
        _logo.Connect(Control.SignalName.MouseExited, Callable.From(OnUnhovered));
    }

    /// <summary>绑定到某个玩家与其当前姿态，刷新图标并定位到角色身上。</summary>
    public void Bind(Player owner, yylStanceModel stance)
    {
        _owner = owner;
        _stance = stance;
        _logo!.Texture = ResourceLoader.Load<Texture2D>(stance.PackedIconPath);
        PositionAboveOwner();
    }

    private void PositionAboveOwner()
    {
        if (_owner == null) return;
        var room = NCombatRoom.Instance;
        if (room == null) return;
        var creatureNode = room.GetCreatureNode(_owner.Creature);
        if (creatureNode == null) return;

        // 命中区：以脚底中心为原点，向上铺满身体
        _hitArea!.GlobalPosition = creatureNode.GlobalPosition + new Vector2(-BodyWidth / 2f, -BodyHeight);
        // logo：命中区左下角再往左一点，避免遮挡角色
        _logo!.GlobalPosition = creatureNode.GlobalPosition +
            new Vector2(-BodyWidth / 2f - LogoSize - LogoGap, -LogoSize - LogoGap);
    }

    private void OnHovered()
    {
        if (_owner == null || _stance == null) return;
        try
        {
            var tip = _stance.DumbHoverTip;
            NCombatRoom.Instance?.GetCreatureNode(_owner.Creature)?.ShowHoverTips(new IHoverTip[] { tip });
        }
        catch
        {
            // 悬浮框仅为锦上添花，任何异常都不应影响战斗。
        }
    }

    private void OnUnhovered()
    {
        if (_owner == null) return;
        try
        {
            NCombatRoom.Instance?.GetCreatureNode(_owner.Creature)?.HideHoverTips();
        }
        catch
        {
            // 悬浮框仅为锦上添花。
        }
    }
}
