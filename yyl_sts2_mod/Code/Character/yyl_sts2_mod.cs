using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using yyl_sts2_mod.Code.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
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

    /*  左下角大能量球上「数字外圈描边」的颜色。
        引擎链路（已反编译 sts2.dll 确认）：
            NEnergyCounter.RefreshLabel
              -> _label.AddThemeColorOverride(FontOutlineColor,
                     Energy > 0 ? get_OutlineColor() : unplayableEnergyCostOutline)
              -> get_OutlineColor()  ==  _player.Character.EnergyLabelOutlineColor

        基类 CharacterModel 默认返回 Color("0000000D") —— 黑色但 alpha 仅 13/255(约 5%)，
        等于「几乎没有描边」，奶黄数字直接贴在大球上缺乏暗色托底。
        静默猎手把这里覆写为深绿 "004f04FF"（注意与其 NameColor 是两套值），
        此处同样覆写为深墨绿。

        重要：这个属性与 NameColor(fbfac2) 完全无关 ——
          * 改这里不会影响角色名颜色；
          * 也不会影响卡牌颜色（卡牌的墨绿来自卡池 H/S/V 着色，见 yyl_sts2_modCardPool）。
        想再暗/再绿，直接改下面这个 16 进制值即可。 */
    public override Color EnergyLabelOutlineColor => new("004f04FF");

    public override CharacterGender Gender => CharacterGender.Masculine;
    public override int StartingHp => 85;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylStrike>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<YylDefend>(),
        ModelDb.Card<PalmThunder>(),
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

    /*  The combat visual is built through the BaseLib node factory instead of relying on
        CustomVisualPath + a C# script on the scene root. Godot's binary scene export strips
        the script ext_resource from the packed scene, which left the root a bare Node2D and
        made CharacterModel.CreateVisuals_Patch2 throw InvalidCastException (Node2D -> NCreatureVisuals).
        NodeFactory<NCreatureVisuals>.CreateFromScene auto-converts the bare Node2D root (which already
        carries the required Visuals / Bounds / IntentPos / CenterPos named children) into a proper
        NCreatureVisuals, which is exactly the pattern LexNinja2 uses and which works. */
    public override NCreatureVisuals CreateCustomVisuals()
    {
        return NodeFactory<NCreatureVisuals>.CreateFromScene("res://yyl_sts2_mod/scenes/yylCharacter.tscn");
    }
    public override string CustomMerchantAnimPath => "res://yyl_sts2_mod/scenes/yylMerchant.tscn";
    public override string CustomRestSiteAnimPath => "res://yyl_sts2_mod/scenes/yylRest.tscn";
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();

    // 底部战斗能量计数器（左下角大球）用自定义场景渲染 bigger_energy.png，
    // 与卡费图标(BigEnergyIconPath=big_energy.png)、文字能量(text_energy.png)三者解耦。
    public override string CustomEnergyCounterPath => "res://yyl_sts2_mod/scenes/yyl_energy_counter.tscn";
}