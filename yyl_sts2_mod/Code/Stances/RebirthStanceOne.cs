using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

/// <summary>
///     逆生一重姿态: 每回合第一次获得格挡时，该次格挡翻倍。
///     <para>
///         [2026-09-20] 翻倍逻辑迁移到隐藏 Power <see cref="Powers.RebirthStanceOnePower"/>
///         (原版坚定不移 UnmovablePower 同款管线) —— 块值分发不吃 ModHelper 订阅的
///         姿态模型, 在姿态上重写 <c>ModifyBlockMultiplicative</c> 从不触发。
///         本类只保留姿态身份 / 悬浮描述 / VFX。Power 的挂载与移除在
///         <c>yylModel.SetStance</c> 里同步。
///     </para>
/// </summary>
public class RebirthStanceOne : yylStanceModel
{
    public override bool ShouldReceiveCombatHooks => true;

    protected override StanceVfxConfig VfxConfig => new(
        BodyTint: new Color(0.85f, 1f, 0.82f),
        EnterSfxPath: yylAudio.Sfx("stance/reverse_life1.ogg"));
}
