using System;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using yyl_sts2_mod.Code;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Extensions;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

public abstract class yylStanceModel : AbstractModel
{
    private Player? _player;

    private StanceVfxController? _vfx;
    public Player Owner => _player ?? throw new InvalidOperationException("Not a mutable instance");

    /// <summary>姿态名称（powers 表 <c>YYL_STS2_MOD-&lt;ENTRY&gt;.title</c>）。
    /// <para>
    /// 重要坑：姿态经 <c>yylModelDb.yylStance&lt;T&gt;()</c> 注册，其 <c>Id.Entry</c> 是
    /// <c>StringHelper.Slugify(类名)</c> 的「无前缀」形式（如 <c>REBIRTH_STANCE_ONE</c>）；
    /// 而 powers 由框架注册、带 <c>YYL_STS2_MOD-</c> 前缀。引擎的 <c>LocString</c> 直接用 key 查表，
    /// 表键是 <c>YYL_STS2_MOD-REBIRTH_STANCE_ONE.title</c>，所以必须手动拼上 <c>MainFile.ModId</c> 前缀，
    /// 否则命中不到键、回退成裸串 <c>powers.REBIRTH_STANCE_ONE.title</c>（实测过）。
    /// 该 LocString 同时供 StanceTagIndicator 悬浮提示与进入横幅使用。
    /// </para></summary>
    public LocString Title => new("powers", $"{MainFile.ModId.ToUpperInvariant()}-{Id.Entry}.title");

    /// <summary>姿态效果描述（同前缀规则，见 <see cref="Title" />）。</summary>
    public LocString Description =>
        new("powers", $"{MainFile.ModId.ToUpperInvariant()}-{Id.Entry}.description");

    /// <summary>姿态图标（与能力图标同一目录、同一命名规则）。</summary>
    public string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();

    /// <summary>姿态大图标；缺图时按 <see cref="StringExtensions.BigPowerImagePath" /> 的规则回退。</summary>
    public string BigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();

    private Texture2D Icon => ResourceLoader.Load<Texture2D>(PackedIconPath);

    public HoverTip DumbHoverTip
    {
        get
        {
            var description = Description;
            AddDumbVariablesToDescription(description);
            return new HoverTip(Title, description.GetFormattedText(), Icon);
        }
    }

    protected abstract StanceVfxConfig VfxConfig { get; }

    public IEnumerable<string> AssetPaths => VfxConfig.AssetPaths;

    public Color? BodyTint => VfxConfig.BodyTint;

    public yylStanceModel ToMutable(Player player)
    {
        var mutable = (yylStanceModel)MutableClone();
        mutable._player = player;
        return mutable;
    }

    private void AddDumbVariablesToDescription(LocString description)
    {
        description.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");
        var pool = IsMutable ? Owner.Character.CardPool : ModelDb.CardPool<yyl_sts2_modCardPool>();
        description.Add("energyPrefix", EnergyIconHelper.GetPrefix(pool));
    }

    public virtual async Task OnEnterStance(PlayerChoiceContext ctx, Player owner, CardModel? source)
    {
        _vfx = new StanceVfxController(VfxConfig);
        try
        {
            await _vfx.OnEnter(owner.Creature);
        }
        catch (Exception e)
        {
            GD.PushError($"[yyl] {GetType().Name}.OnEnterStance VFX failed (stance still entered): {e}");
        }
        if (this is NoStance) return;
        ShowStanceBanner();
    }

    public virtual async Task OnExitStance(PlayerChoiceContext ctx, Player owner, CardModel? source)
    {
        if (_vfx != null)
        {
            try
            {
                await _vfx.OnExit(owner.Creature);
            }
            catch (Exception e)
            {
                GD.PushError($"[yyl] {GetType().Name}.OnExitStance VFX failed: {e}");
            }
            _vfx = null;
        }
    }

    /// <summary>进入姿态时在战斗界面顶部弹出米色横幅 (<c>NCommonBanner</c>) 显示姿态名，2.4s 后淡出。
    /// 观者没有这个，但用户觉得 coool，作为进入姿态的额外反馈。整段包 try/catch，UI 异常绝不阻断战斗流程。</summary>
    private void ShowStanceBanner()
    {
        try
        {
            var scene = GD.Load<PackedScene>("res://scenes/ui/common_banner.tscn");
            if (scene == null || NCombatRoom.Instance is not Node room) return;

            var banner = scene.Instantiate<NCommonBanner>();
            if (banner == null) return;
            room.AddChild(banner);
            banner.TopLevel = true;                       // 忽略父节点变换，按视口坐标定位
            banner.SetAnchorsPreset(Control.LayoutPreset.CenterTop); // 顶部居中

            banner.ChangeText(Title.GetFormattedText());
            banner.AnimateIn();

            var tree = banner.GetTree();
            if (tree == null) return;

            var hold = tree.CreateTimer(2.4);
            hold.Timeout += () =>
            {
                if (!GodotObject.IsInstanceValid(banner)) return;
                banner.AnimateOut();
                var cleanup = banner.GetTree()?.CreateTimer(0.6);
                if (cleanup != null)
                    cleanup.Timeout += () =>
                    {
                        if (GodotObject.IsInstanceValid(banner))
                            banner.QueueFree();
                    };
            };
        }
        catch
        {
            // 横幅仅为锦上添花，任何异常都不应影响战斗。
        }
    }
}
