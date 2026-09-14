using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using yyl_sts2_mod.Code.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using yyl_sts2_mod.Code.Cards.Basic;
using yyl_sts2_mod.Code.Relics;

namespace yyl_sts2_mod.Code.Character;


public class yyl_sts2_mod : PlaceholderCharacterModel
{
    public const string CharacterId = "yyl_sts2_mod";

    public static readonly Color Color = new("fbfac2");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Masculine;
    public override int StartingHp => 85;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<SolarThunder>(),
        ModelDb.Card<GoldenCharm>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<PeachCan>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<yyl_sts2_modCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<yyl_sts2_modRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<yyl_sts2_modPotionPool>();

    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets.
        These are just some of the simplest assets, given some placeholders to differentiate your character with.
        You don't have to, but you're suggested to rename these images. */
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }

    public override string CustomCharacterSelectBg => "res://yyl_sts2_mod/scenes/yylUI.tscn";
    public override string CustomVisualPath => "res://yyl_sts2_mod/scenes/yylCharacter.tscn";
    public override string CustomMerchantAnimPath => "res://yyl_sts2_mod/scenes/yylMerchant.tscn";
    public override string CustomRestSiteAnimPath => "res://yyl_sts2_mod/scenes/yylRest.tscn";
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}