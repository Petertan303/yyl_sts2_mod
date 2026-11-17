using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Powers;
using yyl_sts2_mod.Code.Utils;
using yyl_sts2_mod.Code.Vfx;

namespace yyl_sts2_mod.Code.Stances;

/// <summary>
///     逆生三重：失去逆生二重的每回合收益，**打出当回合立刻**获得 1 层无实体，
///     之后每隔一回合再获得 1 层；没有无实体的回合时保留一重的首格挡翻倍。
///     <para>
///         [2026-10-07 用户定调] 原本靠 <c>BeforeSideTurnStart</c> 发无实体 —— 打出当回合的
///         「回合开始」钩子早已过去，所以必然要等到下个回合才拿到第一层，手感上凭空晚一回合。
///         改为 <see cref="OnEnterStance" /> 里立即给第一层（对齐卡面「获得 1 层无实体」的即时感），
///         并把隔回合计数重置为 0；此后 <c>BeforeSideTurnStart</c> 每过一个己方回合 +1，
///         <b>偶数</b>才发放 —— 于是节奏是「打出的这回合给 / 下回合不给 / 再下回合给」，
///         与卡面文案「每隔一回合获得 1 层无实体」一致。
///     </para>
/// </summary>
public sealed class RebirthStanceThree : RebirthStanceTwo
{
    /// <summary>
    ///     进入三重姿态后已过的己方回合数。<see cref="OnEnterStance" /> 归零，
    ///     故进场当回合 = 0（已即时给过），下一回合 = 1（不发），再下回合 = 2（发）。
    /// </summary>
    private static readonly SpireField<Creature, int> _turnsSinceEnter = new(() => 0);

    protected override StanceVfxConfig VfxConfig => new(
        BodyTint: new Color(1.1f, 0.95f, 0.65f),
        EnterSfxPath: yylAudio.Sfx("stance/reverse_life3.ogg"));

    public override async Task OnEnterStance(PlayerChoiceContext ctx, Player owner, CardModel? source)
    {
        await base.OnEnterStance(ctx, owner, source);

        // 归零后再发：下面的 BeforeSideTurnStart 以"偶数回合才发"为准则，必须从 0 起算，
        // 否则会把进场当回合误算成第 1 回合，导致紧接着的下一个回合又白给一层。
        _turnsSinceEnter[owner.Creature] = 0;
        await PowerCmd.Apply<IntangiblePower>(ctx, owner.Creature, 1m, owner.Creature, source);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext ctx,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        /*  ★2026-11-16 用户定调: 三重「失去逆生二重效果」—— 不执行二重的每回合收益
            (旧实现经 base 链每回合回血 6, 与卡面文案不符), 因此这里**不再调用 base**。
            节奏不变: 进场当回合已即时给过无实体, 此后每个己方回合开始 +1, 偶数发放。
            新增: 没有无实体的回合 (奇数) 保留一重的首格挡翻倍 —— 挂隐藏 power
            <see cref="Powers.RebirthStanceOnePower" /> (历史查询式, 与一重完全同源);
            无实体回合 (偶数) 则移除, 保证翻倍只落在没有无实体的回合。 */
        if (side != Owner.Creature.Side)
            return;

        var n = _turnsSinceEnter[Owner.Creature] + 1;
        _turnsSinceEnter[Owner.Creature] = n;

        var ward = Owner.Creature.GetPower<RebirthStanceOnePower>();
        if (n % 2 == 0)
        {
            await PowerCmd.Apply<IntangiblePower>(ctx, Owner.Creature, 1m, Owner.Creature, null);
            if (ward != null)
            {
                try { await PowerCmd.Remove(ward); }
                catch (Exception e) { MainFile.Logger.Warn($"[yyl] 三重移除首格挡翻倍失败, 已忽略: {e.Message}"); }
            }
        }
        else if (ward == null)
        {
            try
            {
                await PowerCmd.Apply<RebirthStanceOnePower>(
                    ctx, new[] { Owner.Creature }, 1m, Owner.Creature, null);
            }
            catch (Exception e) { MainFile.Logger.Warn($"[yyl] 三重挂载首格挡翻倍失败, 已忽略: {e.Message}"); }
        }
    }
}
