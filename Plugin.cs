using System.Collections.Generic;
using System.IO;
using System.Reflection;
using application;
using baseEffect.graphics;
using BepInEx;
using BepInEx.Logging;
using config;
using coreSystem;
using HarmonyLib;
using resource;
using systemService.trophy;
using UnityEngine;
using System;
using gnosia;
using systemService.saveData;
using setting;

namespace GnosiaCustomizer;

[BepInPlugin("com.sitg.gnosia.customizer", "Gnosia Customizer", "1.0.0")]
[BepInProcess("Gnosia.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private static readonly string[] packedNames = new string[] { "p01", "p02", "p03", "p04", "p05", "p06", "p07", "p08", "p09", "p10", "p11", "p12", "p13", "p14" };
    private static readonly uint[] charSpriteIndeces = new uint[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400 };
    private static readonly string[] headNames = new string[] { "h01", "h02", "h03", "h04", "h05", "h06", "h07" };
    private static readonly uint[] headOffsetIndeces = new uint[] { 1, 2, 3, 4, 5, 6, 7 };

    private static Dictionary<string, CharaTexture> charaTextures = new Dictionary<string, CharaTexture>();
    private static HashSet<uint> modifiedSpriteIndeces = new HashSet<uint>();

    private struct CharaTexture
    {
        public Texture2D texture;
        public Vector2[] sizes;
        public Vector2[] offsets;
    }

    public void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin gnosia customizer is loaded!");

        // Load custom sprites
        LoadCustomSprites();
        Logger.LogInfo($"Custom sprites loaded: {charaTextures.Count} characters with custom textures");

        var harmony = new Harmony("com.sitg.gnosia.customizer");
        harmony.PatchAll();
        Logger.LogInfo($"Harmony patches applied.");
    }

    // Load custom sprites from the "textures" folder
    private void LoadCustomSprites()
    {
        // Load custom sprites from the "textures" folder
        string[] textureFiles = Directory.GetFiles(Path.Combine(Paths.PluginPath, "textures"), "*.png");
        var numTextures = headNames.Length + 1;
        foreach (var chara in packedNames)
        {
            var textures = new Texture2D[numTextures];
            var sizes = new Vector2[numTextures];
            float totalWidth = 0;
            float maxHeight = 0;
            // Check if the file ${chara}_h00.png exists
            if (!File.Exists(Path.Combine(Paths.PluginPath, "textures", $"{chara}_h00.png")))
            {
                continue;
            }
            else
            {
                // Load the texture into textures[0]
                var bodyTexture = new Texture2D(2, 2);
                byte[] fileData = File.ReadAllBytes(Path.Combine(Paths.PluginPath, "textures", $"{chara}_h00.png"));
                if (bodyTexture.LoadImage(fileData))
                {
                    textures[0] = bodyTexture;
                    sizes[0] = new Vector2(bodyTexture.width, bodyTexture.height);
                    totalWidth += bodyTexture.width;
                    maxHeight = Mathf.Max(maxHeight, bodyTexture.height);
                }
                else
                {
                    Logger.LogError($"Failed to load texture {chara}_h00.png");
                    continue;
                }
            }

            var allTexturesExist = true;
            var charaIndex = 1;
            foreach (var head in headNames)
            {
                // Verify that the file ${chara}_{head}.png exists
                if (!File.Exists(Path.Combine(Paths.PluginPath, "textures", $"{chara}_{head}.png")))
                {
                    Logger.LogWarning($"Texture file {chara}_{head}.png not found. Skipping.");
                    allTexturesExist = false;
                    break;
                }
                else
                {
                    // Load into the textures array
                    var headTexture = new Texture2D(2, 2);
                    byte[] fileData = File.ReadAllBytes(Path.Combine(Paths.PluginPath, "textures", $"{chara}_{head}.png"));
                    if (headTexture.LoadImage(fileData))
                    {
                        textures[charaIndex] = headTexture;
                        sizes[charaIndex] = new Vector2(headTexture.width, headTexture.height);
                        totalWidth += headTexture.width;
                        maxHeight = Mathf.Max(maxHeight, headTexture.height);
                    }
                    else
                    {
                        Logger.LogError($"Failed to load texture {chara}_{head}.png");
                        allTexturesExist = false;
                        break;
                    }
                }
                charaIndex++;
            }
            if (!allTexturesExist)
            {
                continue;
            }

            // Create a sprite sheet to contain all of these
            var spriteSheet = new Texture2D((int)totalWidth, (int)maxHeight, TextureFormat.RGBA32, false);

            // Fill with transparent pixels
            var fillColor = Color.clear;
            var fillPixels = new Color[spriteSheet.width * spriteSheet.height];
            for (int i = 0; i < fillPixels.Length; i++)
                fillPixels[i] = fillColor;
            spriteSheet.SetPixels(fillPixels);

            var offsets = new Vector2[numTextures];
            var currentX = 0f;
            for (int i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                var size = sizes[i];
                offsets[i] = new Vector2(currentX, 0);
                currentX += size.x;
                spriteSheet.SetPixels((int)offsets[i].x, (int)offsets[i].y, (int)size.x, (int)size.y, texture.GetPixels());
            }
            spriteSheet.Apply();

            var charaTexture = new CharaTexture
            {
                texture = spriteSheet,
                sizes = sizes,
                offsets = offsets
            };
            charaTextures.Add(chara, charaTexture);

            // Save the sprite sheet to a file for debugging purposes
            //string spriteSheetPath = Path.Combine(Paths.PluginPath, "textures", $"{chara}_spriteSheet.png");
            //File.WriteAllBytes(spriteSheetPath, spriteSheet.EncodeToPNG());
        }
    }

    // Overwrite the offsets and sizes of the custom sprites in their respective sprite sheets
    [HarmonyPatch(typeof(config.Config), nameof(config.Config.Initialize))]
    public static class Initialize_Config_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Config __instance, ref int __result)
        {

            // Iterate through each custom sprite
            foreach (var packedName in charaTextures.Keys)
            {
                var charaTexture = charaTextures[packedName];
                // Log the size of the texture
                var sizes = charaTexture.sizes;
                var offsets = charaTexture.offsets;

                // Clear the original packed map
                foreach (string key2 in __instance.m_packedMap[packedName].Keys)
                    __instance.m_packedMap[packedName][key2].m_child.Clear();
                __instance.m_packedMap[packedName].Clear();

                // Set a new one
                __instance.m_packedMap[packedName] = new Dictionary<string, PackedTexture>()
                {
                    {
                        "body",
                        new PackedTexture(charaTexture.offsets[0], charaTexture.sizes[0],
                        new Dictionary<string, Vector2>(), 0.0f)
                    },
                    {
                        "h01",
                        new PackedTexture(charaTexture.offsets[1], charaTexture.sizes[1],
                        new Dictionary<string, Vector2>(), 0.0f)
                    },
                    {
                        "h02",
                        new PackedTexture(charaTexture.offsets[2], charaTexture.sizes[2],
                        new Dictionary<string, Vector2>(), 0.0f)
                    },
                    {
                        "h03",
                        new PackedTexture(charaTexture.offsets[3], charaTexture.sizes[3],
                        new Dictionary<string, Vector2>(), 0.0f)
                    },
                    {
                        "h04",
                        new PackedTexture(charaTexture.offsets[4], charaTexture.sizes[4],
                        new Dictionary<string, Vector2>(), 0.0f)
                    },
                    {
                        "h05",
                        new PackedTexture(charaTexture.offsets[5], charaTexture.sizes[5],
                        new Dictionary<string, Vector2>(), 0.0f)
                    },
                    {
                        "h06",
                        new PackedTexture(charaTexture.offsets[6], charaTexture.sizes[6],
                        new Dictionary<string, Vector2>(), 0.0f)
                    },
                    {
                        "h07",
                        new PackedTexture(charaTexture.offsets[1], charaTexture.sizes[7],
                        new Dictionary<string, Vector2>(), 0.0f)
                    },
                };
            }
        }
    }

    // Add the textures into the sprite map
    [HarmonyPatch(typeof(CharaScreen), nameof(CharaScreen.InitializeGlm))]
    public static class CharaScreen_InitializeGlm_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(CharaScreen __instance, ResourceManager resourceManager, ScriptParser scriptParser, GameLogManager gameLogManager)
        {

            for (uint charIndex = 0; charIndex < packedNames.Length; charIndex++)
            {
                var packedName = packedNames[charIndex];
                if (!charaTextures.ContainsKey(packedName))
                {
                    Logger.LogWarning($"Custom sprite {packedName} not found. Skipping.");
                    continue;
                }

                var textureName = "body";
                var spriteIndex = charSpriteIndeces[charIndex];

                var position = new Vector2?(new Vector2((float)(50.0 * charIndex - 200.0), 0.0f));
                // Body

                __instance.SetPackedTexture(
                    0,
                    __instance.transform,
                    packedName,
                    textureName,
                    spriteIndex,
                    order: 10U,
                    position,
                    character: true);
                modifiedSpriteIndeces.Add(spriteIndex);

                __instance.m_spriteMap[spriteIndex].SetSize(0.7f);
                __instance.m_spriteMap[spriteIndex].GetComponent<UnityEngine.UI.Image>().material = resourceManager.uiCharaDefaultMat;
                __instance.m_spriteMap[spriteIndex].SetDisplayOffsetY((float)resourceManager.m_displaySize.height - __instance.m_spriteMap[spriteIndex].GetSizeInDisplay().y * __instance.m_spriteMap[spriteIndex].GetSize() * GraphicsContext.m_textureRatio);
                __instance.m_spriteMap[spriteIndex].GetComponent<UnityEngine.UI.Image>().material.SetColor("_Color", (Color)__instance.GetColorCoeff());

                // Heads
                for (int headIndex = 0; headIndex < headNames.Length; headIndex++)
                {
                    var headTextureName = headNames[headIndex];
                    var headSpriteIndex = spriteIndex + headOffsetIndeces[headIndex];
                    __instance.SetPackedTexture(
                        0,
                        __instance.transform,
                        packedName,
                        headTextureName,
                        headSpriteIndex,
                        order: 1U,
                        position);
                    modifiedSpriteIndeces.Add(headSpriteIndex);

                    __instance.m_spriteMap[headSpriteIndex].SetSize(0.7f);
                    __instance.m_spriteMap[headSpriteIndex].GetComponent<UnityEngine.UI.Image>().material = resourceManager.uiCharaDefaultMat;
                    __instance.m_spriteMap[headSpriteIndex].SetDisplayOffsetY((float)resourceManager.m_displaySize.height - __instance.m_spriteMap[spriteIndex].GetSizeInDisplay().y * __instance.m_spriteMap[spriteIndex].GetSize() * GraphicsContext.m_textureRatio);
                    __instance.m_spriteMap[headSpriteIndex].GetComponent<UnityEngine.UI.Image>().material.SetColor("_Color", (Color)__instance.GetColorCoeff());
                }
            }
        }
    }

    // Overwrite the original resource with the custom sprite sheet
    [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetTexture))]
    public static class GetTexture_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(string resourceName, ref ResourceManager.ResTextureList __result)
        {
            // Load texture from custom sprites if it exists
            if (charaTextures.ContainsKey(resourceName))
            {
                var charaTexture = charaTextures[resourceName];

                __result = new ResourceManager.ResTextureList()
                {
                    count = 1,
                    isFixed = false,
                    slot = 0,
                    userInfo = new GraphicsContext.TextureUserInfo()
                    {
                        size = new Vector2(charaTexture.texture.width, charaTexture.texture.height),
                        isMadeInGame = false
                    },
                    texture = charaTexture.texture
                };
            }
        }
    }

    // Display the given sprite on the screen. For custom sprites, do not attempt layering.
    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.ShowChara))]
    public static class ShowChara_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ScriptParser __instance, ref int __result, int chara, int hyojo, int pos = 0, uint depth = 20, bool charaisId = false)
        {
            if (chara > 0)
            {
                // We need to calculate the sprite index to determine if this should be a custom sprite.
                int thyojo = hyojo % 100;
                var gameData = GetGameDataViaReflection();
                if (gameData == null)
                {
                    Logger.LogWarning("Failed to get GameData");
                    return true;
                }
                int tid = charaisId ? chara : (int)gameData.chara[chara].id;

                var spriteIndex = thyojo > 0 ? tid * 100U + hyojo : tid * 100U;
                if (!modifiedSpriteIndeces.Contains((uint)spriteIndex))
                {
                    return true;
                }

                // For custom sprites, do not draw the default sprite underneath
                __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                {
                    var sprite = __instance.m_sb[depth].m_spriteMap[(uint)spriteIndex];
                    sprite.SetVisible(true);
                    sprite.SetCenterPosition(
                        new Vector2((float)(__instance.m_rs.m_displaySize.width / 4 * (pos + 1)) + sprite.m_faceCenter * sprite.GetSize(), sprite.GetCenterPosition().y));
                    return true;
                }), (ScriptParser.Script._EndFunc)(e => true), false));
            }
            __result = 1;
            return false;
        }
    }

    // Ensures that both heads and bodies are hidden now that they can be separate
    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.UnvisibleAllChara))]
    public static class UnvisibleAllChara_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ScriptParser __instance, ref int __result, uint depth = 20, int chara = -1)
        {
            const uint numHeads = 7U;
            if (chara <= 0)
                __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                {
                    for (uint index = 1; index < 15U; ++index)
                    {
                        var spriteIndex = index * 100U;
                        if (__instance.m_sb[depth].m_spriteMap.ContainsKey(index * 100U))
                        {
                            __instance.m_sb[depth].m_spriteMap[index * 100U].UnvisibleWithChild();
                        }
                        // Also iterate through every head
                        for (uint headIndex = 1; headIndex < numHeads; ++headIndex)
                        {
                            if (__instance.m_sb[depth].m_spriteMap.ContainsKey(spriteIndex + headIndex))
                            {
                                __instance.m_sb[depth].m_spriteMap[spriteIndex + headIndex].UnvisibleWithChild();
                            }
                        }
                    }
                    return true;
                }), (ScriptParser.Script._EndFunc)(e => true), false));
            else
                __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                {
                    var gameData = GetGameDataViaReflection();
                    if (gameData == null)
                    {
                        Logger.LogWarning("Failed to cast gd instance to GameData");
                        return true;
                    }

                    var spriteIndex = gameData.chara[chara].id * 100U;
                    __instance.m_sb[depth].m_spriteMap[spriteIndex].UnvisibleWithChild();

                    // Also iterate through every head
                    for (uint headIndex = 1; headIndex < numHeads; ++headIndex)
                    {
                        if (__instance.m_sb[depth].m_spriteMap.ContainsKey(spriteIndex + headIndex))
                        {
                            __instance.m_sb[depth].m_spriteMap[spriteIndex + headIndex].UnvisibleWithChild();
                        }
                    }
                    return true;
                }), (ScriptParser.Script._EndFunc)(e => true), false));
            __result = 1;
            return false;
        }
    }

    // void CharaScreen.SetColorCoeff(Vector4 color)
    [HarmonyPatch(typeof(CharaScreen), nameof(CharaScreen.SetColorCoeff))]
    public static class SetColorCoeff_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(CharaScreen __instance, Vector4 color)
        {
            return true;
        }
    }

    private static GameData? GetGameDataViaReflection()
    {
        // Use reflection to get Data
        Type dataType = AccessTools.TypeByName("gnosia.Data");
        FieldInfo gdField = dataType?.GetField("gd", BindingFlags.Public | BindingFlags.Static);
        if (gdField == null)
        {
            return null;
        }
        object gdInstance = gdField.GetValue(null);
        var gameData = gdInstance as GameData;
        return gameData;
    }
}
