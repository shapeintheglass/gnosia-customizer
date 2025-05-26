using System.Collections.Generic;
using System.IO;
using System.Linq;
using application;
using baseEffect.graphics;
using BepInEx;
using BepInEx.Logging;
using config;
using coreSystem;
using GnosiaCustomizer.utils;
using HarmonyLib;
using resource;
using systemService.trophy;
using UnityEngine;

namespace GnosiaCustomizer
{
    internal class SpritePatches : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private static readonly string[] packedNames = new string[] { "p01", "p02", "p03", "p04", "p05", "p06", "p07", "p08", "p09", "p10", "p11", "p12", "p13", "p14" };
        private static readonly uint[] charSpriteIndeces = new uint[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400 };
        private static readonly string[] headNames = new string[] { "h01", "h02", "h03", "h04", "h05", "h06", "h07" };
        private static readonly uint[] headOffsetIndeces = new uint[] { 1, 2, 3, 4, 5, 6, 7 };
        private const string bgMainConsoleName = "bg_mainConsole.png";

        private static Dictionary<string, CharaTexture> charaTextures = new Dictionary<string, CharaTexture>();
        private static HashSet<uint> modifiedSpriteIndeces = new HashSet<uint>();
        private static Dictionary<string, ResourceManager.ResTextureList> bgTextures;

        private struct CharaTexture
        {
            public ResourceManager.ResTextureList texture;
            public Vector2[] sizes;
            public Vector2[] offsets;
            public Vector2? position;
        }

        // Load custom sprites from the "textures" folder
        internal static void LoadCustomSprites(ManualLogSource logger)
        {
            logger.LogInfo("LoadCustomSprites() called");
            // Verify that textures folder exists
            string texturesPath = Path.Combine(Paths.PluginPath, "textures");
            if (!Directory.Exists(texturesPath))
            {
                logger.LogError($"Textures folder not found at {texturesPath}. Please create a 'textures' folder in the plugin directory and add your custom sprites.");
                return;
            }

            // Backgrounds
            var bgMainConsolePath = Path.Combine(texturesPath, bgMainConsoleName);
            if (File.Exists(bgMainConsolePath))
            {
                logger.LogInfo($"Loading background texture: {bgMainConsolePath}");
                var bgConsoleTexture = new Texture2D(2, 2); // This will get overwritten by the actual texture
                byte[] bgFileData = File.ReadAllBytes(bgMainConsolePath);
                if (bgConsoleTexture.LoadImage(bgFileData))
                {
                    bgTextures = new Dictionary<string, ResourceManager.ResTextureList>()
                            {
                                { bgMainConsoleName, new ResourceManager.ResTextureList()
                                    {
                                        count = 1,
                                        isFixed = false,
                                        slot = 0,
                                        userInfo = new GraphicsContext.TextureUserInfo()
                                        {
                                            size = new Vector2(bgConsoleTexture.width, bgConsoleTexture.height),
                                            isMadeInGame = false
                                        },
                                        texture = bgConsoleTexture
                                    }
                                }
                            };
                }
                else
                {
                    logger.LogError($"Failed to load background texture {bgMainConsoleName}");
                }
            }

            // Sprites
            var availableFiles = new HashSet<string>(Directory.GetFiles(texturesPath, "*.png", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName));
            // Load custom sprites from the "textures" folder
            var numTextures = headNames.Length + 1;
            var charIndex = 0;
            foreach (var chara in packedNames)
            {
                var textures = new Texture2D[numTextures];
                var sizes = new Vector2[numTextures];
                float totalWidth = 0;
                float maxHeight = 0;
                string bodyFile = $"{chara}_h00.png";
                // Check if the file ${chara}_h00.png exists
                if (!availableFiles.Contains(bodyFile))
                {
                    Logger.LogInfo($"Body file not found: {bodyFile}");
                    continue;
                }

                // Load the texture into textures[0]
                var bodyTexture = new Texture2D(2, 2);
                byte[] fileData = File.ReadAllBytes(Path.Combine(Paths.PluginPath, "textures", bodyFile));
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


                var allTexturesExist = true;
                var charaIndex = 1;
                foreach (var head in headNames)
                {
                    var headFile = $"{chara}_{head}.png";
                    // Verify that the file ${chara}_{head}.png exists
                    if (!availableFiles.Contains(headFile))
                    {
                        Logger.LogInfo($"Head file not found: {headFile}");
                        allTexturesExist = false;
                        break;
                    }

                    // Load into the textures array
                    var headTexture = new Texture2D(2, 2);
                    byte[] headFileData = File.ReadAllBytes(Path.Combine(Paths.PluginPath, "textures", headFile));
                    if (headTexture.LoadImage(headFileData))
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
                {
                    fillPixels[i] = fillColor;
                }
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

                var resourceTextureList = new ResourceManager.ResTextureList
                {
                    count = 1,
                    isFixed = false,
                    slot = 0,
                    userInfo = new GraphicsContext.TextureUserInfo
                    {
                        size = new Vector2(spriteSheet.width, spriteSheet.height),
                        isMadeInGame = false
                    },
                    texture = spriteSheet
                };

                var charaTexture = new CharaTexture
                {
                    texture = resourceTextureList,
                    sizes = sizes,
                    offsets = offsets,
                    position = new Vector2?(new Vector2((float)(50.0 * charIndex - 200.0), 0.0f))
                };
                charaTextures.Add(chara, charaTexture);
                charIndex++;
            }
            Logger.LogInfo($"Custom sprites loaded: {charaTextures.Count} characters with custom textures");
        }

        [HarmonyPatch(typeof(config.Config), nameof(config.Config.Initialize))]
        public static class Initialize_Config_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Config __instance, ref int __result)
            {
                Logger.LogInfo($"Initialize_Config_Patch.Postfix called (__instance: {__instance?.GetType().Name}, __result: {__result})");
                // Iterate through each custom sprite
                foreach (var packedName in charaTextures.Keys)
                {
                    var charaTexture = charaTextures[packedName];
                    var sizes = charaTexture.sizes;
                    var offsets = charaTexture.offsets;

                    __instance.m_packedMap[packedName] = new Dictionary<string, PackedTexture>()
                {
                    {
                        "body",
                        new PackedTexture(charaTexture.offsets[0], charaTexture.sizes[0], [], 0.0f)
                    },
                    {
                        "h01",
                        new PackedTexture(charaTexture.offsets[1], charaTexture.sizes[1], [], 0.0f)
                    },
                    {
                        "h02",
                        new PackedTexture(charaTexture.offsets[2], charaTexture.sizes[2], [], 0.0f)
                    },
                    {
                        "h03",
                        new PackedTexture(charaTexture.offsets[3], charaTexture.sizes[3], [], 0.0f)
                    },
                    {
                        "h04",
                        new PackedTexture(charaTexture.offsets[4], charaTexture.sizes[4], [], 0.0f)
                    },
                    {
                        "h05",
                        new PackedTexture(charaTexture.offsets[5], charaTexture.sizes[5], [], 0.0f)
                    },
                    {
                        "h06",
                        new PackedTexture(charaTexture.offsets[6], charaTexture.sizes[6], [], 0.0f)
                    },
                    {
                        "h07",
                        new PackedTexture(charaTexture.offsets[7], charaTexture.sizes[7], [], 0.0f)
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
                Logger.LogInfo($"CharaScreen_InitializeGlm_Patch.Postfix called (__instance: {__instance?.GetType().Name}, resourceManager: {resourceManager?.GetType().Name}, scriptParser: {scriptParser?.GetType().Name}, gameLogManager: {gameLogManager?.GetType().Name})");
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

                    var position = charaTextures[packedName].position;
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

                    var bodySprite = __instance.m_spriteMap[spriteIndex];
                    bodySprite.SetSize(0.7f);
                    bodySprite.GetComponent<UnityEngine.UI.Image>().material = resourceManager.uiCharaDefaultMat;
                    bodySprite.SetDisplayOffsetY((float)resourceManager.m_displaySize.height - bodySprite.GetSizeInDisplay().y * bodySprite.GetSize() * GraphicsContext.m_textureRatio);
                    bodySprite.GetComponent<UnityEngine.UI.Image>().material.SetColor("_Color", (Color)__instance.GetColorCoeff());

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

                        var headSprite = __instance.m_spriteMap[headSpriteIndex];
                        headSprite.SetSize(0.7f);
                        headSprite.GetComponent<UnityEngine.UI.Image>().material = resourceManager.uiCharaDefaultMat;
                        headSprite.SetDisplayOffsetY((float)resourceManager.m_displaySize.height - bodySprite.GetSizeInDisplay().y * bodySprite.GetSize() * GraphicsContext.m_textureRatio);
                        headSprite.GetComponent<UnityEngine.UI.Image>().material.SetColor("_Color", (Color)__instance.GetColorCoeff());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetTexture))]
        public static class GetTexture_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(string resourceName, ref ResourceManager.ResTextureList __result)
            {
                Logger.LogInfo($"GetTexture_Patch.Prefix called (resourceName: {resourceName}, __result: {(__result != null ? __result.GetType().Name : "null")})");
                // Load texture from custom sprites if it exists
                if (charaTextures.ContainsKey(resourceName))
                {
                    __result = charaTextures[resourceName].texture;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.ShowChara))]
        public static class ShowChara_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(ScriptParser __instance, ref int __result, int chara, int hyojo, int pos = 0, uint depth = 20, bool charaisId = false)
            {
                Logger.LogInfo($"ShowChara_Patch.Prefix called (__instance: {__instance?.GetType().Name}, chara: {chara}, hyojo: {hyojo}, pos: {pos}, depth: {depth}, charaisId: {charaisId})");
                if (chara > 0)
                {
                    // We need to calculate the sprite index to determine if this should be a custom sprite.
                    int thyojo = hyojo % 100;
                    var gameData = Utils.GetGameDataViaReflection();
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

        [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.UnvisibleAllChara))]
        public static class UnvisibleAllChara_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(ScriptParser __instance, ref int __result, uint depth = 20, int chara = -1)
            {
                Logger.LogInfo($"UnvisibleAllChara_Patch.Prefix called (__instance: {__instance?.GetType().Name}, depth: {depth}, chara: {chara})");
                const uint numHeads = 7U;
                if (chara <= 0)
                    __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                    {
                        for (uint index = 1; index < 15U; ++index)
                        {
                            var spriteIndex = index * 100U;
                            if (__instance.m_sb[depth].m_spriteMap.ContainsKey(spriteIndex))
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
                        var gameData = Utils.GetGameDataViaReflection();
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

        [HarmonyPatch(typeof(application.Screen), nameof(application.Screen.SetTexture))]
        public static class SetTexture_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(application.Screen __instance, ref int __result, int textureType, Transform parentTrans, uint depth, string textureName, Vector2? _position = null, ResourceManager.ResTextureList texture = null)
            {
                Logger.LogInfo($"SetTexture_Patch.Prefix called (__instance: {__instance?.GetType().Name}, textureType: {textureType}, parentTrans: {parentTrans?.GetType().Name}, depth: {depth}, textureName: {textureName}, _position: {_position}, texture: {(texture != null ? texture.GetType().Name : "null")})");
                if (textureName == "bg_mainConsole")
                {
                    Vector2 display = _position ?? Vector2.zero;
                    GameObject gameObject = new GameObject(textureName);
                    gameObject.AddComponent<Sprite2dEffectArg>();
                    gameObject.AddComponent<UnityEngine.UI.Image>();
                    gameObject.transform.SetParent(parentTrans);
                    gameObject.SetActive(false);
                    __instance.m_spriteMap[depth] = gameObject.GetComponent<Sprite2dEffectArg>();
                    var resourceManager = Utils.GetResourceManagerViaReflection(__instance);
                    if (resourceManager == null)
                    {
                        Logger.LogWarning("Failed to get ResourceManager instance via reflection");
                        return true;
                    }
                    __instance.m_spriteMap[depth].SetFromLeftUpper(textureType, display, bgTextures[bgMainConsoleName], resourceManager.uiDefaultMat);
                    __result = 1;
                    return false;
                }
                return true;
            }
        }
    }
}
