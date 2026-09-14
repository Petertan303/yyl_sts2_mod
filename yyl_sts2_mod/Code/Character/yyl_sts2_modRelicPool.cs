using BaseLib.Abstracts;
using yyl_sts2_mod.Code.Extensions;
using Godot;

namespace yyl_sts2_mod.Code.Character;

public class yyl_sts2_modRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => yyl_sts2_mod.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}