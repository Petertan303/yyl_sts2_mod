using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace yyl_sts2_mod.Code.Patches;

/*  ★伊林角色的攻击音效 (2026-09-30)。
    背景: 原版 CharacterModel.AttackSfx 是按角色 Id 拼的非虚属性
        "event:/sfx/characters/{Id小写}/{Id小写}_attack"
    mod 角色的 Id 是 "YYL_STS2_MOD-YYL_STS2_MOD" → 拼出不存在的 FMOD 事件 → 攻击时无声。
    BaseLib 的 CustomAttackSfx 属性没有任何消费者(反编译确认, 死属性), 所以只能补丁。
    选 silent_attack (拔刀/短刃挥砍) 贴合伊林的居合形象; 该事件随原版必然存在。 */
[HarmonyPatch(typeof(CharacterModel), "AttackSfx", MethodType.Getter)]
internal static class YylAttackSfxPatch
{
    private const string YylAttackSfx = "event:/sfx/characters/silent/silent_attack";

    [HarmonyPostfix]
    internal static void Postfix(CharacterModel __instance, ref string __result)
    {
        if (__instance is Character.yyl_sts2_mod)
        {
            __result = YylAttackSfx;
        }
    }
}
