using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Extensions;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

public abstract class yylStanceModel : AbstractModel
{
    private Player? _player;

    private StanceVfxController? _vfx;
    public Player Owner => _player ?? throw new InvalidOperationException("Not a mutable instance");

    private LocString Title => new("powers", $"{MainFile.ModId.ToUpperInvariant()}-{Id.Entry}.title");

    private LocString Description =>
        new("powers", $"{MainFile.ModId.ToUpperInvariant()}-{Id.Entry}.description");

    private string PackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();
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
        await _vfx.OnEnter(owner.Creature);
    }

    public virtual async Task OnExitStance(PlayerChoiceContext ctx, Player owner, CardModel? source)
    {
        if (_vfx != null)
            await _vfx.OnExit(owner.Creature);
        _vfx = null;
    }
}

