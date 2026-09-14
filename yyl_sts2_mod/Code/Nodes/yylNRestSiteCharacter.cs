using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace yyl_sts2_mod.Code.Nodes;

[GlobalClass]
public partial class yylNRestSiteCharacter : NRestSiteCharacter
{
    // public override void _Ready()
    // {
    //     base._Ready();
    //     var isFlipped = _characterIndex % 2 == 1;
    //     var node = GetNode<TextureRect>("SpineSprite");
    //     if (isFlipped) node.FlipH = true;
    // }
}