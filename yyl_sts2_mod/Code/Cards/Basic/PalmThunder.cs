using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;
using yyl_sts2_mod.Code.Cards.Ancient;
using yyl_sts2_mod.Code.Character;
using yyl_sts2_mod.Code.Powers;
// using RitsuLib.Interop.AutoRegistration;

namespace yyl_sts2_mod.Code.Cards.Basic;

/// <summary>
///     掌心雷: 初始牌 (原「阳五雷」)。
///     1 费, 先施加 1 → 2 层易伤, 再造成 3 → 4 点伤害 2 次;
///     若身上有金光护体, 消耗 1 层使本次每段伤害 +1 (与白长虫同款联动)。
///     <para>
///         结算顺序: 易伤 → 攻击 → 消耗金光护体 (攻击时仍在身上, 能吃到加成)。
///         白长虫 (WhiteWorm) 是独立先古卡, 通过「古老牙齿」获得, 不在升级时变形。
///     </para>
/// </summary>
[Pool(typeof(yyl_sts2_modCardPool))]
// 若日后接「古老牙齿」(Ancient Tooth) 遗物，应在此注册对应先古牌:
// [RegisterArchaicToothTranscendence(typeof(WhiteWorm))]
#pragma warning disable STS004
public sealed class PalmThunder(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType,
    bool shouldShowInCardLibrary = true)
    : yylCardModel(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
{
    public PalmThunder() : this(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
        // 主伤害 = 3 → 4 (基础 3, 升级 +1)。金光护体 +1 在 OnPlay 内按实际状态结算,
        // 卡面 {Damage} 用普通 DamageVar (战斗外 IConvertible 返回 BaseValue=3, 不再显示 0/花括号)。
        WithDamage(3, 1);
        WithVars(new RepeatVar(2));
        // 卡面"本次每段伤害提高 {Bonus} 点": 普通 DynamicVar, 战斗外显示预期值 1。
        WithVar("Bonus", 1, 0);
        WithPower<VulnerablePower>(1, 1);
        // 金光护体联动: 声明 -1 层供 ApplySelf 消耗, 与白长虫 (WhiteWorm) 同一写法。
        WithPower<GoldenAegis>(-1);
        // 仅用于卡面显示: 这次要消耗几层
        WithPower<GoldenAegis>("AegisCost", 1, 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        if (target == null) return;

        // 1. 先施加易伤 (让本次攻击直接吃到 1.5x)。
        await CommonActions.Apply<VulnerablePower>(choiceContext, new[] { target }, this);

        // 2. 攻击: 基础伤害取普通 DamageVar, 金光护体 +1 在这里按实际状态结算
        //    (卡面 {Damage}=3 与 {Bonus}=1 已给出预期值, OnPlay 才是真正结算)。
        var hasAegis = Owner.HasPower<GoldenAegis>();
        var damage = DynamicVars["Damage"].IntValue + (hasAegis ? 1 : 0);
        await CommonActions.CardAttack(this, cardPlay, target, damage,
                ValueProp.Move, hitCount: DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_lightning")
            .Execute(choiceContext);

        // 3. 攻击结算完再消耗 1 层金光护体 (保证本次攻击已经吃到 +1)。
        if (Owner.HasPower<GoldenAegis>())
            await CommonActions.ApplySelf<GoldenAegis>(choiceContext, this);
    }
}
