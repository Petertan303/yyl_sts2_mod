// using System.Reflection;
// using System.Reflection.Emit;
// using HarmonyLib;
// using MegaCrit.Sts2.Core.Entities.Cards;
// using MegaCrit.Sts2.Core.Entities.Creatures;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.ValueProps;
//
// namespace yyl_sts2_mod.Code.Patches;
//
// // ========== 接口定义 ==========
// public interface IModifyBlockAdditive
// {
//     decimal ModifyBlockAdditiveCompability(Creature target, decimal blockAmount,
//         ValueProp props, Creature? source, CardModel? cardSource) => 0m;
// }
//
// public interface IModifyBlockMultiplicative
// {
//     decimal ModifyBlockMultiplicativeCompability(Creature target, decimal blockAmount,
//         ValueProp props, Creature? source, CardModel? cardSource) => 1m;
// }
//
// // ========== Harmony 注入补丁 ==========
// [HarmonyPatch]
// internal static class ModifyBlockPatch
// {
//     private static MethodBase TargetMethod()
//     {
//         var hookType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Hooks.Hook")
//                        ?? throw new TypeLoadException("Hook type not found");
//         // 建议加上参数类型精确匹配，避免重载歧义
//         var method = AccessTools.Method(hookType, "ModifyBlock", new[]
//         {
//             typeof(Creature),   // target
//             typeof(decimal),    // blockAmount
//             typeof(ValueProp),  // props
//             typeof(Creature),   // source
//             typeof(CardModel)   // cardSource
//         });
//         return method ?? throw new MissingMethodException("Target block method not found");
//     }
//
//     // 加算桥接：在官方原生加算结果上叠加自定义加算值
//     private static decimal AdditiveBridge(AbstractModel listener, decimal vanillaNum,
//         Creature target, decimal amount, ValueProp props, Creature? source,
//         CardModel? cardSource)
//     {
//         if (listener is IModifyBlockAdditive m)
//             return vanillaNum + m.ModifyBlockAdditiveCompability(target, amount, props, source, cardSource);
//         return vanillaNum;
//     }
//
//     // 乘算桥接：在官方原生乘算结果上叠加自定义倍率
//     private static decimal MultiplicativeBridge(AbstractModel listener, decimal vanillaNum,
//         Creature target, decimal amount, ValueProp props, Creature? source,
//         CardModel? cardSource)
//     {
//         if (listener is IModifyBlockMultiplicative m)
//             return vanillaNum * m.ModifyBlockMultiplicativeCompability(target, amount, props, source, cardSource);
//         return vanillaNum;
//     }
//
//     private static IEnumerable<CodeInstruction> Transpiler(
//         IEnumerable<CodeInstruction> instructions, MethodBase original)
//     {
//         var code = new List<CodeInstruction>(instructions);
//
//         // 定位官方原生的格挡修改方法
//         var addMethod = AccessTools.Method(typeof(AbstractModel), "ModifyBlockAdditive");
//         var mulMethod = AccessTools.Method(typeof(AbstractModel), "ModifyBlockMultiplicative");
//         var addBridge = AccessTools.Method(typeof(ModifyBlockPatch), nameof(AdditiveBridge));
//         var mulBridge = AccessTools.Method(typeof(ModifyBlockPatch), nameof(MultiplicativeBridge));
//
//         // 遍历指令，在官方每次调用格挡修改后插入我们的自定义逻辑
//         for (var i = 0; i < code.Count; i++)
//         {
//             var isAdd = code[i].Calls(addMethod);
//             var isMul = code[i].Calls(mulMethod);
//             if (!isAdd && !isMul) continue;
//
//             var storeIndex = i + 1;
//             if (storeIndex >= code.Count || code[storeIndex].opcode != OpCodes.Stloc_S) continue;
//             var numLocal = code[storeIndex].operand;
//
//             // 向后查找当前遍历的 listener 实例加载指令
//             var listenerLoad = FindListenerLoadBackwards(code, i);
//             if (listenerLoad == null) continue;
//
//             // 注入自定义接口调用
//             var injected = new List<CodeInstruction>
//             {
//                 listenerLoad.Clone(),
//                 new(OpCodes.Ldloc_S, numLocal),   // 官方计算后的原始数值
//                 new(OpCodes.Ldarg_2),             // target 目标生物
//                 new(OpCodes.Ldloc_0),             // blockAmount 格挡数值
//                 new(OpCodes.Ldarg_S, (byte)5),    // props 属性标签
//                 new(OpCodes.Ldarg_3),             // source 格挡来源
//                 new(OpCodes.Ldarg_S, (byte)6),    // cardSource 来源卡牌
//                 new(OpCodes.Call, isAdd ? addBridge : mulBridge),
//                 new(OpCodes.Stloc_S, numLocal),   // 写回计算结果
//             };
//
//             code.InsertRange(storeIndex + 1, injected);
//             i = storeIndex + injected.Count;
//         }
//
//         return code;
//     }
//
//     // 从调用点向前回溯，找到当前 listener 的加载指令
//     private static CodeInstruction? FindListenerLoadBackwards(List<CodeInstruction> code, int callIndex)
//     {
//         for (var j = callIndex - 1; j >= 0 && j > callIndex - 10; j--)
//         {
//             if (code[j].opcode == OpCodes.Ldarg_2)
//                 return code[j - 1];
//         }
//         return null;
//     }
// }