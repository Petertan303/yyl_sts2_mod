using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

/// <summary>
///     逆生二重：保留一重的首格挡翻倍（SetStance 不再移除一重的隐藏 power），
///     玩家回合开始获得 8 点格挡。
///     <para>
///         ★2026-11-16 用户定调: 每回合收益由「回血 6」改为「<b>获得 8 点格挡</b>」,
///         与卡牌 RebirthTwo 的立即 8 格挡同值 (RebirthTwo.StanceBlock)。
///     </para>
/// </summary>
public class RebirthStanceTwo : RebirthStanceOne
{
    /// <summary>每回合开始获得的格挡量 (与 RebirthTwo.StanceBlock 同值, 互为卡面/结算锚点)。</summary>
    public const decimal PerTurnBlock = 8m;

    protected override StanceVfxConfig VfxConfig => new(
        BodyTint: new Color(0.7f, 1f, 0.9f),
        EnterSfxPath: yylAudio.Sfx("stance/reverse_life2.ogg"));

    // public override async Task BeforeSideTurnStart(
    //     PlayerChoiceContext ctx,
    //     CombatSide side,
    //     IReadOnlyList<Creature> participants,
    //     ICombatState combatState)
    // {
    //     await base.BeforeSideTurnStart(ctx, side, participants, combatState);
    //     if (side == Owner.Creature.Side)
    //         // Unpowered: 不吃卡牌来源的首格挡翻倍与力量类加成 (姿态类格挡既定口径)。
    //         await CreatureCmd.GainBlock(Owner.Creature, PerTurnBlock, ValueProp.Unpowered, null);
    // }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        CreatureCmd.GainBlock(Owner.Creature, PerTurnBlock, ValueProp.Unpowered, null);
        return base.AfterPlayerTurnStart(choiceContext, player);
    }
}
