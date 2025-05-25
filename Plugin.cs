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

            var charaTexture = new CharaTexture
            {
                texture = spriteSheet,
                sizes = sizes,
                offsets = offsets
            };
            charaTextures.Add(chara, charaTexture);

            // Save the sprite sheet to a file for debugging purposes
            string spriteSheetPath = Path.Combine(Paths.PluginPath, "textures", $"{chara}_spriteSheet.png");
            File.WriteAllBytes(spriteSheetPath, spriteSheet.EncodeToPNG());
            Logger.LogInfo($"Sprite sheet saved to {spriteSheetPath}");
            Logger.LogInfo($"Loaded custom sprite for {chara} with size: {spriteSheet.width} x {spriteSheet.height} and offsets: {string.Join(", ", offsets)}");
        }
    }

    [HarmonyPatch(typeof(config.Config), nameof(config.Config.Initialize))]
    public static class Initialize_Config_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Config __instance, ref int __result)
        {
            Logger.LogInfo($"Config.Initialize called, result: {__result}");

            // Iterate through each custom sprite
            foreach (var packedName in charaTextures.Keys)
            {
                var charaTexture = charaTextures[packedName];
                // Log the size of the texture
                var sizes = charaTexture.sizes;
                var offsets = charaTexture.offsets;
                Logger.LogInfo($"Initializing config for custom sprite: {packedName}. num sizes: {sizes.Length} num offsets: {offsets.Length}");

                for (int i = 0; i < sizes.Length; i++)
                {
                    Logger.LogInfo($"Texture {i}: size: {sizes[i]}, offset: {offsets[i]}");
                }

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

    [HarmonyPatch(typeof(CharaScreen), nameof(CharaScreen.InitializeGlm))]
    public static class CharaScreen_InitializeGlm_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(CharaScreen __instance, ResourceManager resourceManager, ScriptParser scriptParser, GameLogManager gameLogManager)
        {
            Logger.LogInfo("CharaScreen.InitializeGlm patch called");

            for (uint charIndex = 0; charIndex < packedNames.Length; charIndex++)
            {
                var packedName = packedNames[charIndex];
                var textureName = "body";
                var spriteIndex = charSpriteIndeces[charIndex];

                var position = new Vector2?(new Vector2((float)(50.0 * charIndex - 200.0), 0.0f));
                // Body
                Logger.LogInfo($"Processing character index: {charIndex} packedName: {packedName} textureName: {textureName} spriteIndex: {spriteIndex} position: {position}");

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

                // Write to png
                var bodyTexture = CopyTextureReadable(__instance.m_spriteMap[spriteIndex].m_texture.texture);
                var bodyTexturePath = Path.Combine(Paths.PluginPath, "textures", $"{packedName}_{spriteIndex}.png");
                File.WriteAllBytes(bodyTexturePath, bodyTexture.EncodeToPNG());

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
                    // Write to png
                    var head = CopyTextureReadable(__instance.m_spriteMap[headSpriteIndex].m_texture.texture);
                    var headTexturePath = Path.Combine(Paths.PluginPath, "textures", $"{packedName}_{headTextureName}_{spriteIndex}.png");
                    File.WriteAllBytes(headTexturePath, head.EncodeToPNG());
                }
            }
        }
    }

    private static Texture2D CopyTextureReadable(Texture2D source)
    {
        // Create a temporary RenderTexture the same size as the source
        var rt = RenderTexture.GetTemporary(
            source.width, source.height,
            0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

        // Blit (GPU copy) the source texture into our RT
        Graphics.Blit(source, rt);

        // Remember the currently active RT, then bind ours
        var previous = RenderTexture.active;
        RenderTexture.active = rt;

        // Read the pixels from the RT into a new Texture2D
        var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();

        // Cleanup: restore the original RT and release ours
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return copy;
    }


    [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetTexture))]
    public static class GetTexture_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(string resourceName, ref ResourceManager.ResTextureList __result)
        {
            Logger.LogInfo($"GetTexture called with resourceName: {resourceName}");
            if (__result != null)
            {
                // Log the count, isFixed, slot, userInfo, and textureName of the ResTextureList
                Logger.LogInfo($"GetTexture result: count: {__result.count}, isFixed: {__result.isFixed}, slot: {__result.slot}, userInfo.size: {__result.userInfo.size}, userInfo.isMadeInGame: {__result.userInfo.isMadeInGame}, textureName: {__result.texture.width} x {__result.texture.height}");
            }
            else
            {
                Logger.LogWarning($"GetTexture result is null for resourceName: {resourceName}");
            }

            // Load texture from custom sprites if it exists
            if (charaTextures.ContainsKey(resourceName))
            {
                Logger.LogInfo($"Loading custom sprite sheet for resourceName: {resourceName}");
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



    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.ShowChara))]
    public static class ShowChara_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ScriptParser __instance, ref int __result, int chara, int hyojo, int pos = 0, uint depth = 20, bool charaisId = false)
        {
            Logger.LogInfo($"ShowChara called with chara: {chara}, hyojo: {hyojo}, pos: {pos}, depth: {depth}, charaisId: {charaisId}");


            if (chara > 0)
            {
                int thyojo = hyojo % 100;

                // Use reflection to get Data
                Type dataType = AccessTools.TypeByName("gnosia.Data");
                FieldInfo gdField = dataType?.GetField("gd", BindingFlags.Public | BindingFlags.Static);
                if (gdField == null)
                {
                    Logger.LogWarning("Failed to find gnosia.Data type or gd field");
                    return true;
                }
                object gdInstance = gdField.GetValue(null);
                var gameData = gdInstance as GameData;
                if (gameData == null)
                {
                    Logger.LogWarning("Failed to cast gd instance to GameData");
                    return true;
                }
                int tid = charaisId ? chara : (int)gameData.chara[chara].id;
                Logger.LogInfo($"tid = {tid}");

                var spriteIndex = thyojo > 0 ? tid * 100U + hyojo : tid * 100U;
                if (!modifiedSpriteIndeces.Contains((uint)spriteIndex))
                {
                    Logger.LogInfo($"Skipping sprite index {spriteIndex} as it has not been modified.");
                    return true;
                }

                __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                {
                    Logger.LogInfo($"Using sprite index: {spriteIndex}");
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

    // ScriptParser.UnvisibleAllChara(uint depth = 20, int chara = -1)
    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.UnvisibleAllChara))]
    public static class UnvisibleAllChara_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ScriptParser __instance, ref int __result, uint depth = 20, int chara = -1)
        {
            Logger.LogInfo($"UnvisibleAllChara called with depth: {depth}, chara: {chara}");
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
                    // Use reflection to get Data
                    Type dataType = AccessTools.TypeByName("gnosia.Data");
                    FieldInfo gdField = dataType?.GetField("gd", BindingFlags.Public | BindingFlags.Static);
                    if (gdField == null)
                    {
                        Logger.LogWarning("Failed to find gnosia.Data type or gd field");
                        return true;
                    }
                    object gdInstance = gdField.GetValue(null);
                    var gameData = gdInstance as GameData;
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

    #region system

    // public void GameData.GetFromBaseData(ref SaveDataManager.SaveDataFileImage image)
    [HarmonyPatch(typeof(GameData), nameof(GameData.GetFromBaseData))]
    public static class GetFromBaseData_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(GameData __instance, ref SaveDataManager.SaveDataFileImage image)
        {
            Logger.LogInfo($"GetFromBaseData called with image: {image}");
            foreach (var character in __instance.chara)
            {
                Logger.LogInfo($"ID: {character.id}, doa: {character.doa}");
            }
        }
    }

    // GameData.MakeLoop()
    [HarmonyPatch(typeof(GameData), nameof(GameData.MakeLoop))]
    public static class MakeLoop_Patch
    {
        [HarmonyPostfix]
        public static void Prefix(GameData __instance)
        {
            Logger.LogInfo($"MakeLoop called with __instance: {__instance}");
            foreach (var chara in __instance.chara)
            {
                Logger.LogInfo($"Chara: {chara.id}");
            }
        }
    }

    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.LoadPlace))]
    public static class LoadPlace_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(byte place = 255, bool setPlaceData = true)
        {
            Logger.LogInfo($"LoadPlace called with place: {place}, setPlaceData: {setPlaceData}");
        }
    }

    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.SetInterface))]
    public static class SetInterface_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(uint depth, int mainChara, int targetChara = -1, bool vRole = true, bool shouldLog = true)
        {
            Logger.LogInfo($"SetInterface called with depth: {depth}, mainChara: {mainChara}, targetChara: {targetChara}, vRole: {vRole}, shouldLog: {shouldLog}");
        }
    }

    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.SetNormalSerifu))]
    public static class SetNormalSerifu_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(int main, int tgt, int pos, List<string> lang, bool waitNextText = false, bool withoutTrans = false, bool withoutCharaChange = false, bool vRole = true)
        {
            Logger.LogInfo($"SetNormalSerifu called with main: {main}, tgt: {tgt}, pos: {pos}, lang: {string.Join(", ", lang)}, waitNextText: {waitNextText}, withoutTrans: {withoutTrans}, withoutCharaChange: {withoutCharaChange}, vRole: {vRole}");
        }
    }

    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.SetDialogScreen))]
    public static class SetDialogScreen_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(uint depth, string message, int lines, bool canSelect)
        {
            Logger.LogInfo($"SetDialogScreen called with depth: {depth}, message: {message}, lines: {lines}, canSelect: {canSelect}");
        }
    }

    [HarmonyPatch(typeof(application.Screen), nameof(application.Screen.SetTexture))]
    public static class SetTexture_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(int textureType, Transform parentTrans, uint depth, string textureName, Vector2? _position = null, ResourceManager.ResTextureList texture = null)
        {
            Logger.LogInfo($"SetTexture called with type: {textureType}, depth: {depth}, textureName: {textureName}, position: {_position}");
        }
    }

    [HarmonyPatch(typeof(application.Screen), nameof(application.Screen.SetPackedTexture))]
    public static class SetPackedTexture_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(
            application.Screen __instance,
            int type,
            Transform parentTrans,
            string packedName,
            string textureName,
            uint depth,
            uint order = 100,
            Vector2? _position = null,
            Sprite2dEffectArg parent = null,
            ResourceManager.ResTextureList texture = null,
            bool character = false)
        {
            Logger.LogInfo($"SetPackedTexture called with type: {type}, packedName: {packedName}, textureName: {textureName}, depth: {depth}, order: {order}, position: {_position}, character: {character}");
        }
    }

    

    #endregion system
    #region texture
    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.LoadTexture))]
    public static class LoadTexture_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(string resourceName)
        {
            Logger.LogInfo($"LoadTexture called with resourceName: {resourceName}");
        }
    }

    // public int ResourceManager.LoadTexture(string resourceName, bool isFixed = false)
    [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.LoadTexture))]
    public static class LoadTexture_ResourceManager_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(string resourceName, bool isFixed = false)
        {
            Logger.LogInfo($"LoadTexture (ResourceManager) called with resourceName: {resourceName}, isFixed: {isFixed}");
        }
    }

    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.ChangeCharaTexture))]
    public static class ChangeCharaTexture_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(uint cid, string textureName, uint order = 10, uint targetLayer = 20, bool matUse = true)
        {
            Logger.LogInfo($"ChangeCharaTexture called with cid: {cid}, textureName: {textureName}, order: {order}, targetLayer: {targetLayer}, matUse: {matUse}");
        }
    }

    [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.SetCharaSingleTexture))]
    public static class SetCharaSingleTexture_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(int _depth, string textureName, uint pos = 1, float faceCenter = 0.0f, uint targetLayer = 20)
        {
            Logger.LogInfo($"SetCharaSingleTexture called with _depth: {_depth}, textureName: {textureName}, pos: {pos}, faceCenter: {faceCenter}, targetLayer: {targetLayer}");
        }
    }
    #endregion texture

    #region sound
    //[HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.LoadSound))]
    //public static class LoadSound_Patch
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix(string resourceName)
    //    {
    //        Logger.LogInfo($"LoadSound called with resourceName: {resourceName}");
    //    }
    //}

    //[HarmonyPatch(typeof(SoundManager), nameof(SoundManager.PlayBgm))]
    //public static class PlayBgm_Patch
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix(string name, float fadeInTime = 0.0f, float volume = 1f, int target = -1, bool loop = true)
    //    {
    //        Logger.LogInfo($"PlayBgm called with name: {name}, fadeInTime: {fadeInTime}, volume: {volume}, target: {target}, loop: {loop}");
    //    }
    //}

    //[HarmonyPatch(typeof(SoundManager), nameof(SoundManager.StopBgm))]
    //public static class StopBgm_Patch
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix(int target = -1, bool isOld = false)
    //    {
    //        Logger.LogInfo($"StopBgm called with target: {target}, isOld: {isOld}");
    //    }
    //}

    //[HarmonyPatch(typeof(SoundManager), nameof(SoundManager.FadeBgm))]
    //public static class FadeBgm_Patch
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix(float startVol, float lastVol, float fadeTime, bool stopAtFadeOut = false, int target = -1)
    //    {
    //        Logger.LogInfo($"FadeBgm called with startVol: {startVol}, lastVol: {lastVol}, fadeTime: {fadeTime}, stopAtFadeOut: {stopAtFadeOut}, target: {target}");
    //    }
    //}

    //[HarmonyPatch(typeof(SoundManager), nameof(SoundManager.PlaySe))]
    //public static class PlaySe_Patch
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix(string name, float volume = 1f)
    //    {
    //        Logger.LogInfo($"PlaySe called with name: {name}, volume: {volume}");
    //    }
    //}

    //[HarmonyPatch(typeof(SoundManager), nameof(SoundManager.StopAllSe))]
    //public static class StopAllSe_Patch
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix()
    //    {
    //        Logger.LogInfo($"StopAllSe called");
    //    }
    //}
    
    #endregion sound
}
