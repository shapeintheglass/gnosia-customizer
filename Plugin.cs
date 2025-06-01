using System.IO;
using BepInEx;
using GnosiaCustomizer.patches;
using GnosiaCustomizer.utils;
using HarmonyLib;

namespace GnosiaCustomizer;

[BepInPlugin(PluginId, PluginName, Version)]
[BepInProcess(GnosiaProcessName)]
public class Plugin : BaseUnityPlugin
{
    private const string PluginId = "com.sitg.gnosia.customizer";
    private const string PluginName = "Gnosia Customizer";
    private const string Version = "1.0.0";
    private const string GnosiaProcessName = "Gnosia.exe";

    public void Awake()
    {
        // Verify that assets folder exists
        string assetsPath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder);
        if (!Directory.Exists(assetsPath))
        {
            Logger.LogError($"Gnosia Customizer Assets folder not found at {assetsPath}. Please create this folder and add your custom assets.");
            return;
        }

        SpritePatches.Logger = Logger;
        TextPatches.Logger = Logger;
        SoundPatches.Logger = Logger;
        JinroPatches.Logger = Logger;

        Logger.LogInfo($"Plugin gnosia customizer is starting!");

        SpritePatches.Initialize();
        TextPatches.Initialize();
        SoundPatches.Initialize();

        var harmony = new Harmony(PluginId);
        harmony.PatchAll();
        Logger.LogInfo($"Harmony patches applied.");
    }
}
