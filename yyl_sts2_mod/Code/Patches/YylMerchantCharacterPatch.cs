using System;
using System.Reflection;
using BaseLib.Utils.NodeFactories;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace yyl_sts2_mod.Code.Patches;

/// <summary>
///     Fixes the missing character in the Shop (Merchant Room).
///     <para>
///         RitsuLib patches <c>NMerchantRoom.AfterRoomIsLoaded</c> and builds the shop character with
///         <c>PackedScene.Instantiate&lt;NMerchantCharacter&gt;()</c>. That is a hard cast: it only succeeds if the
///         scene root already IS an <see cref="NMerchantCharacter" />. Our merchant scene root is a plain
///         <c>Node2D</c> (C# scripts attached to scene roots do not resolve inside a mod .pck, even with
///         <c>ScriptManagerBridge.LookupScriptsInAssembly</c>), so the cast throws
///         InvalidCastException and the shop character ends up missing.
///     </para>
///     <para>
///         BaseLib's <c>NodeFactory&lt;NMerchantCharacter&gt;.CreateFromScene</c> auto-converts a bare Node2D root
///         into a real NMerchantCharacter — this is exactly what already makes the combat visuals and the rest
///         site work. This patch intercepts RitsuLib's factory method for our character and uses that instead.
///     </para>
///     No-op when RitsuLib is not installed.
/// </summary>
public static class YylMerchantCharacterPatch
{
    private const string MerchantScenePath = "res://yyl_sts2_mod/scenes/yylMerchant.tscn";

    private const string RitsuPatchTypeName =
        "STS2RitsuLib.Scaffolding.Characters.Patches.NMerchantRoomProceduralCharacterInstantiationPatch";

    private const string RitsuMethodName = "CreateMerchantCharacter";

    public static void TryApply(Harmony harmony)
    {
        try
        {
            MethodInfo? target = FindRitsuMethod();
            if (target == null)
            {
                MainFile.Logger.Info(
                    "YylMerchantCharacterPatch: RitsuLib merchant patch not found; nothing to do (shop uses the default path).");
                return;
            }

            MethodInfo? prefix = typeof(YylMerchantCharacterPatch).GetMethod(nameof(Prefix),
                BindingFlags.Public | BindingFlags.Static);
            if (prefix == null)
            {
                MainFile.Logger.Error("YylMerchantCharacterPatch: could not resolve own Prefix method.");
                return;
            }

            harmony.Patch(target, new HarmonyMethod(prefix));
            MainFile.Logger.Info("YylMerchantCharacterPatch: OK - merchant creation intercepted for yyl.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"YylMerchantCharacterPatch: FAILED to apply.\n{ex}");
        }
    }

    private static MethodInfo? FindRitsuMethod()
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? type = asm.GetType(RitsuPatchTypeName, throwOnError: false);
            if (type == null) continue;

            return type.GetMethod(RitsuMethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        }

        return null;
    }

    /// <summary>
    ///     Returns false (skipping RitsuLib's original) only for our own character; everything else is untouched.
    /// </summary>
    public static bool Prefix(CharacterModel character, ref NMerchantCharacter __result)
    {
        if (character is not global::yyl_sts2_mod.Code.Character.yyl_sts2_mod)
            return true; // not ours -> let RitsuLib do its thing

        __result = NodeFactory<NMerchantCharacter>.CreateFromScene(MerchantScenePath);
        if (__result == null)
            MainFile.Logger.Error(
                $"YylMerchantCharacterPatch: NodeFactory returned null for {MerchantScenePath}.");

        return false; // skip the original Instantiate<T>, which would throw InvalidCastException
    }
}
