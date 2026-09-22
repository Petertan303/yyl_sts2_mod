using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Utils;

namespace yyl_sts2_mod.Code.Cards.Rare;

/// <summary>
///     冰蚕寒功 (原名阴煞): 0 费, 给予所有敌人 99 层虚弱 (≈ 贯穿整场战斗)。消耗。
///     <para>
///         [balance 2026-09-19] 用户定调: 2 费不消耗的常驻全场虚弱过强,
///         改为 0 费 + 消耗 —— 一场战斗只能享受一次, 但当回合完全免费。
///         与风后奇门 (全场 1→2 层可反复) 区分: 这张是"一次性冻住全场"。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
public sealed class YinSha(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public YinSha() : this(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
        WithPower<WeakPower>(99);
        WithCostUpgradeBy(-1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await yylAnim.TriggerCast(this); // 打出动作: 施法帧动画
        var enemies = CombatState?.HittableEnemies ?? [];
        yylVfx.OnCreatures(enemies, "vfx/vfx_smoke_puff"); // 打出特效: 阴煞弥漫
        var weak = DynamicVars["WeakPower"].IntValue;
        if (weak > 0)
            await PowerCmd.Apply<WeakPower>(choiceContext, enemies, weak, Owner.Creature, cardPlay.Card);
    }
}
