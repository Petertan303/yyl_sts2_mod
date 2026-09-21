using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using yyl_sts2_mod.Code.Abstract;

namespace yyl_sts2_mod.Code.Powers;

public class GoldenAegis : yylPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // ⚠ 减伤挂点 (2026-09-20 重做): 此前实现为 IModifyDamageAdditive (伤害管线加法阶段),
    //   但伤害管线在格挡后有"至少掉 1 血"的下限钳制 —— 层数再高也剩 1 点, 减不到 0。
    //   现改为 override 原版掉血修正钩子 ModifyHpLostAfterOstyLate (原版遗物「钨合金棍」
    //   TungstenRod / BufferPower 同款阶段): 伤害 → 格挡 → 表观掉血(下限1) → 本钩子,
    //   在这里可以减到 0。
    //   代价: 敌方意图预览不再显示金光减免后的数字 (钨合金棍同款行为)。
    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || props.HasFlag(ValueProp.Unpowered))
            return amount;

        // 只减"敌方来源"的掉血: dealer 为空 (中毒/状态牌/自伤类 LoseHp) 或来自己方
        // (联机误伤) 时不减 —— 否则燃炁/拉伤这类固定代价也会被金光吃掉。
        if (dealer == null || dealer.Side == Owner.Side)
            return amount;

        // 基础每层减伤 1 点 (2026-09-21 用户定调: 2 太强); 「守势」会再往上加 (每层 +1)。
        var perStack = 1m + (Owner.GetPower<ShouShi>()?.Amount ?? 0m);
        // 减到 0 为止 (掉血阶段没有下限钳制, 这是与伤害阶段实现的本质区别)。
        return Math.Max(0m, amount - Amount * perStack);
    }
}
