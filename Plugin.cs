using System;
using BepInEx;
using BepInEx.Logging;
using coreSystem;
using HarmonyLib;
using UnityEngine;

namespace GnosiaCustomizer;

[BepInPlugin("com.sitg.gnosia.customizer", "Gnosia Customizer", "1.0.0")]
[BepInProcess("Gnosia.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin gnosia customizer is loaded!");

        var harmony = new Harmony("com.sitg.gnosia.customizer");
        harmony.PatchAll();
        Logger.LogInfo($"Harmony patches applied.");
    }

    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.LoadTexture))]
    public static class LoadTexture_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(string resourceName)
        {
            Logger.LogInfo($"LoadTexture called with resourceName: {resourceName}");
        }
    }
}
