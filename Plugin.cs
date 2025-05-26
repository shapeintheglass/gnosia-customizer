using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using application;
using baseEffect.graphics;
using BepInEx;
using BepInEx.Logging;
using config;
using coreSystem;
using gnosia;
using GnosiaCustomizer.utils;
using HarmonyLib;
using resource;
using systemService.trophy;
using UnityEngine;

namespace GnosiaCustomizer;

[BepInPlugin("com.sitg.gnosia.customizer", "Gnosia Customizer", "1.0.0")]
[BepInProcess("Gnosia.exe")]
public class Plugin : BaseUnityPlugin
{
    public void Awake()
    {
        SpritePatches.Logger = Logger;
        // Plugin startup logic
        Logger.LogInfo($"Plugin gnosia customizer is loaded!");

        // Load custom sprites
        SpritePatches.LoadCustomSprites(base.Logger);

        var harmony = new Harmony("com.sitg.gnosia.customizer");
        harmony.PatchAll();
        Logger.LogInfo($"Harmony patches applied.");
    }


}
