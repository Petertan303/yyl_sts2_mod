using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Extensions;
using Godot;

namespace yyl_sts2_mod.Code.Character;

public class yyl_sts2_modCardPool : CustomCardPoolModel
{
    public override string Title => yyl_sts2_mod.CharacterId; //This is not a display name.

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();


    /* These HSV values will determine the color of your card back.
    They are applied as a shader onto an already colored image,
    so it may take some experimentation to find a color you like.
    Generally they should be values between 0 and 1. */
    // public override float H => 1f; //Hue; changes the color.
    // public override float S => 1f; //Saturation
    // public override float V => 1f; //Brightness
    
    public override float H => 0.2f;
    public override float S => 1f;
    public override float V => 1f;

    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load yyl_sts2_mod/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("fbfac2");

    public override bool IsColorless => false;
}