using BepInEx;
using GnosiaCustomizer.patches;
using GnosiaCustomizer.utils;
using HarmonyLib;

namespace GnosiaCustomizer;

[BepInPlugin("com.sitg.gnosia.customizer", "Gnosia Customizer", "1.0.0")]
[BepInProcess("Gnosia.exe")]
public class Plugin : BaseUnityPlugin
{
    private static readonly string[] SetCharaMethodNames = new string[] {
       "SetTakashi", "SetGina", "SetSQ", "SetRakio", "SetStella", "SetSigemichi", "SetCipi", "SetRemnant", "SetComet", "SetYuriko", "SetJonas", "SetSetsu", "SetOtome", "SetShaMin", "SetKukulsika"
    };

    public void Awake()
    {
        SpritePatches.Logger = Logger;
        TextPatches.Logger = Logger;
        SoundPatches.Logger = Logger;
        // Plugin startup logic
        Logger.LogInfo($"Plugin gnosia customizer is loaded!");

        MainThreadDispatcher.Init();

        // Initialize patches and load custom resources
        SpritePatches.InitializeAsync();
        TextPatches.Initialize();
        SoundPatches.Initialize();

        var harmony = new Harmony("com.sitg.gnosia.customizer");

        //foreach (var name in SetCharaMethodNames)
        //{
        //    TextPatches.PatchSetCharaData(harmony, name);
        //}

        harmony.PatchAll();
        Logger.LogInfo($"Harmony patches applied.");
    }


}
