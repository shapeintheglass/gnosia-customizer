using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
using UnityEngine.UI;
using UnityEngine.UIElements.StyleSheets.Syntax;

namespace GnosiaCustomizer
{
    internal class SpritePatches : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private static readonly string[] packedNames = ["p01", "p02", "p03", "p04", "p05", "p06", "p07", "p08", "p09", "p10", "p11", "p12", "p13", "p14"];
        private static readonly uint[] charSpriteIndeces = [100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400];
        private static readonly string[] headNames = ["h01", "h02", "h03", "h04", "h05", "h06", "h07"];
        private static readonly uint[] headOffsetIndeces = [1, 2, 3, 4, 5, 6, 7];
        private const string bgMainConsoleName = "bg_mainConsole.png";

        private static Dictionary<string, CharaTexture> charaTextures = new Dictionary<string, CharaTexture>();
        private static HashSet<uint> modifiedSpriteIndeces = new HashSet<uint>();
        private static Dictionary<string, ResourceManager.ResTextureList> bgTextures;

        private static readonly Type Sprite2dEffectArgType = typeof(Sprite2dEffectArg);
        private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo DisplayOffsetField = Sprite2dEffectArgType.GetField("m_displayOffset", PrivateInstance);
        private static readonly FieldInfo DisplayOffsetObjField = Sprite2dEffectArgType.GetField("m_displayOffsetObj", PrivateInstance);
        private static readonly FieldInfo SizeInDisplayField = Sprite2dEffectArgType.GetField("m_sizeInDisplay", PrivateInstance);
        private static readonly FieldInfo TextureOffsetField = Sprite2dEffectArgType.GetField("m_textureOffset", PrivateInstance);
        private static readonly FieldInfo SizeInTextureField = Sprite2dEffectArgType.GetField("m_sizeInTexture", PrivateInstance);
        private static readonly FieldInfo ImageField = Sprite2dEffectArgType.GetField("m_image", PrivateInstance);
        private static readonly Vector2 ZeroOne = new Vector2(0.0f, 1.0f);
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        private struct CharaFileInfo
        {
            public string bodyFileName;
            public byte[] bodyBytes;
            public string[] headFileNames;
            public string[] headBytes;
            public Vector2[] sizes;
        }

        private struct CharaTexture
        {
            public ResourceManager.ResTextureList texture;
            public Vector2[] sizes;
            public Vector2[] offsets;
            public Vector2? position;
        }

        // Load custom sprites from the "textures" folder
        internal static async Task InitializeAsync()
        {
            // Verify that textures folder exists
            string texturesPath = Path.Combine(Paths.PluginPath, "textures");
            if (!Directory.Exists(texturesPath))
            {
                Logger.LogError($"Textures folder not found at {texturesPath}. Please create a 'textures' folder in the plugin directory and add your custom sprites.");
                return;
            }
            var availableFiles = new HashSet<string>(Directory.GetFiles(texturesPath, "*.png", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName));
            // Backgrounds
            if (availableFiles.Contains(bgMainConsoleName))
            {
                var bgMainConsolePath = Path.Combine(texturesPath, bgMainConsoleName);
                Logger.LogInfo($"Loading background texture: {bgMainConsolePath}");
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
                    Logger.LogError($"Failed to load background texture {bgMainConsoleName}");
                }
            }

            // Sprites
            await LoadCharacterTexturesAsync(availableFiles);
        }

        public static async Task LoadCharacterTexturesAsync(HashSet<string> availableFiles)
        {
            var tasks = new List<Task>();

            var charIndex = 0;
            var numTextures = headNames.Length + 1;
            var spriteNameAndBytes = new List<CharaFileInfo>(numTextures);

            foreach (var chara in packedNames)
            {
                var currentIndex = charIndex++;
                tasks.Add(Task.Run(() =>
                {
                    var sizes = new Vector2[numTextures];

                    string bodyFile = $"{chara}_h00.png";
                    if (!availableFiles.Contains(bodyFile))
                    {
                        Logger.LogInfo($"Body file not found: {bodyFile}");
                        return;
                    }

                    var bodyFilePath = Path.Combine(Paths.PluginPath, "textures", bodyFile);
                    var bodyBytes = File.ReadAllBytes(bodyFilePath);

                    var headsData = new List<(string name, byte[] data)>();

                    foreach (var head in headNames)
                    {
                        var headFile = $"{chara}_{head}.png";
                        if (!availableFiles.Contains(headFile))
                        {
                            Logger.LogInfo($"Head file not found: {headFile}");
                            return;
                        }
                        var headFilePath = Path.Combine(Paths.PluginPath, "textures", headFile);
                        headsData.Add((headFile, File.ReadAllBytes(headFilePath)));
                    }
                }));
            }


            await Task.WhenAll(tasks);
        }

        private static void CreateTextures(int charaIndex, CharaFileInfo info, int numTextures)
        {
            var textures = new Texture2D[numTextures];

            var index = 0;
            float totalWidth = 0;
            float maxHeight = 0;
            var bodyTex = new Texture2D(2, 2);
            if (bodyTex.LoadImage(info.bodyBytes))
            {
                textures[0] = bodyTex;
                info.sizes[0] = new Vector2(bodyTex.width, bodyTex.height);
                totalWidth += bodyTex.width;
                maxHeight = Mathf.Max(maxHeight, bodyTex.height);
            }
            else
            {
                Logger.LogError($"Failed to load body texture for chara index {charaIndex}");
                return;
            }

            index = 1;
            for (;index < numTextures; index++)
            {
                var headTex = new Texture2D(2, 2);
                if (headTex.LoadImage(data))
                {
                    textures[index] = headTex;
                    info.sizes[index] = new Vector2(headTex.width, headTex.height);
                    totalWidth += headTex.width;
                    maxHeight = Mathf.Max(maxHeight, headTex.height);
                }
                else
                {
                    Logger.LogError($"Failed to load texture {name}");
                    return;
                }
                index++;
            }

            // Create sprite sheet
            var spriteSheet = new Texture2D((int)totalWidth, (int)maxHeight, TextureFormat.RGBA32, false);
            var fill = new Color[spriteSheet.width * spriteSheet.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = Color.clear;
            spriteSheet.SetPixels(fill);

            var offsets = new Vector2[numTextures];
            var currentX = 0f;
            for (int i = 0; i < textures.Length; i++)
            {
                var tex = textures[i];
                var size = sizes[i];
                offsets[i] = new Vector2(currentX, 0);
                currentX += size.x;
                spriteSheet.SetPixels((int)offsets[i].x, (int)offsets[i].y, (int)size.x, (int)size.y, tex.GetPixels());
            }
            spriteSheet.Apply();

            var resourceList = new ResourceManager.ResTextureList
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
                texture = resourceList,
                sizes = sizes,
                offsets = offsets,
                position = new Vector2?(new Vector2(50f * currentIndex - 200f, 0f))
            };

            charaTextures[chara] = charaTexture;
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
                var displayHeight = resourceManager.m_displaySize.height;
                var textureRatio = GraphicsContext.m_textureRatio;
                var defaultMat = resourceManager.uiCharaDefaultMat;
                var colorCoeff = (Color)__instance.GetColorCoeff();
                for (uint charIndex = 0; charIndex < packedNames.Length; charIndex++)
                {
                    var packedName = packedNames[charIndex];
                    if (!charaTextures.TryGetValue(packedName, out CharaTexture value))
                    {
                        Logger.LogWarning($"Custom sprite {packedName} not found. Skipping.");
                        continue;
                    }
                    var spriteIndex = charSpriteIndeces[charIndex];
                    var position = value.position ?? Vector2.zero;
                    // Body
                    SetPackedTextureWithCache(
                        __instance,
                        resourceManager,
                        __instance.transform,
                        packedName,
                        "body",
                        spriteIndex,
                        10U,
                        position,
                        value.texture,
                        defaultMat,
                        colorCoeff,
                        displayHeight,
                        textureRatio);
                    modifiedSpriteIndeces.Add(spriteIndex);

                    // Heads
                    for (int headIndex = 0; headIndex < headNames.Length; headIndex++)
                    {
                        var headTextureName = headNames[headIndex];
                        var headSpriteIndex = spriteIndex + headOffsetIndeces[headIndex];
                        SetPackedTextureWithCache(
                            __instance,
                            resourceManager,
                            __instance.transform,
                            packedName,
                            headTextureName,
                            headSpriteIndex,
                            1U,
                            position,
                            charaTextures[packedName].texture,
                            defaultMat,
                            colorCoeff,
                            displayHeight,
                            textureRatio);
                        modifiedSpriteIndeces.Add(headSpriteIndex);
                    }
                }
            }
        }

        private static void SetPackedTextureWithCache(application.Screen __instance,
                ResourceManager rm,
                Transform parentTrans,
                string packedName,
                string textureName,
                uint depth,
                uint order,
                Vector2 _position,
                ResourceManager.ResTextureList texture,
                Material mat,
                Color colorCoeff,
                float displayHeight,
                float textureRatio)
        {
            if (!rm.m_config.m_packedMap.TryGetValue(packedName, out var packedTextures) 
                || !packedTextures.TryGetValue(textureName, out var textureConfig))
            {
                Logger.LogWarning($"Packed texture {packedName}/{textureName} not found in config.");
                return;
            }

            if (DisplayOffsetField == null
                || DisplayOffsetObjField == null
                || SizeInDisplayField == null
                || TextureOffsetField == null
                || SizeInTextureField == null
                || ImageField == null)
            {
                throw new Exception($"One or more field types in Sprite2dEffectArg could not be found." +
                    $"DisplayOffsetField: {DisplayOffsetField == null}, DisplayOffsetObjField: {DisplayOffsetObjField == null} " +
                    $"SizeInDisplayField: {SizeInDisplayField == null}, TextureOffsetField: {TextureOffsetField == null} " +
                    $"SizeInTextureField: {SizeInTextureField == null}, ImageField: {ImageField == null}");
            }
            GameObject gameObject = new GameObject(textureName);
            gameObject.transform.SetParent(parentTrans);
            gameObject.SetActive(false);

            // Game object sprite
            var sprite = gameObject.AddComponent<Sprite2dEffectArg>();
            __instance.m_spriteMap[depth] = sprite;
            sprite.m_type = 0;
            sprite.m_texture = texture;
            DisplayOffsetField.SetValue(sprite, _position);
            var displayOffsetVec = new Vector2(_position.x / 3f * 4f, _position.y / 3f * 4f * -1f);
            DisplayOffsetObjField.SetValue(sprite, displayOffsetVec);
            SizeInDisplayField.SetValue(sprite, textureConfig.m_sizeInTexture);
            TextureOffsetField.SetValue(sprite, textureConfig.m_textureOffset);
            SizeInTextureField.SetValue(sprite, textureConfig.m_sizeInTexture);

            // Game object image
            var image = gameObject.AddComponent<Image>();
            image.material = mat;
            image.material.SetColor("_Color", colorCoeff);
            var cachedName = $"{packedName}_{textureName}";
            if (!spriteCache.TryGetValue(cachedName, out var cachedSprite))
            {
                var newTextureOffset = new Vector2(textureConfig.m_textureOffset.x,
                texture.texture.height - (textureConfig.m_textureOffset.y + textureConfig.m_sizeInTexture.y));
                cachedSprite = Sprite.Create(sprite.m_texture.texture, new Rect(newTextureOffset, textureConfig.m_sizeInTexture),
                ZeroOne);
                spriteCache[cachedName] = cachedSprite;
            }
            image.sprite = cachedSprite;
            image.rectTransform.sizeDelta = textureConfig.m_sizeInTexture;
            image.rectTransform.anchorMax = ZeroOne;
            image.rectTransform.anchorMin = ZeroOne;
            image.rectTransform.pivot = ZeroOne;
            image.rectTransform.anchoredPosition3D = (Vector3)displayOffsetVec;
            image.rectTransform.localScale = Vector3.one;
            ImageField.SetValue(sprite, image);

            sprite.SetSize(0.7f);
            sprite.SetDisplayOffsetY(displayHeight - sprite.GetSizeInDisplay().y * sprite.GetSize() * textureRatio);
        }

        [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetTexture))]
        public static class GetTexture_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(string resourceName, ref ResourceManager.ResTextureList __result)
            {
                // Load texture from custom sprites if it exists
                if (charaTextures.ContainsKey(resourceName))
                {
                    Logger.LogInfo($"GetTexture_Patch.Prefix called (resourceName: {resourceName})");
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
                Logger.LogInfo($"ShowChara_Patch.Prefix called (chara: {chara}, hyojo: {hyojo}, pos: {pos}, depth: {depth}, charaisId: {charaisId})");
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
                        // Check if this is present in the sprite map anyways
                        if (!__instance.m_sb.ContainsKey(depth) || !__instance.m_sb[depth].m_spriteMap.ContainsKey((uint)spriteIndex))
                        {
                            Logger.LogWarning($"{spriteIndex} is NOT in the sprite map! Oh no!");
                            return false;
                        }
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
                Logger.LogInfo($"UnvisibleAllChara_Patch.Prefix called (depth: {depth}, chara: {chara})");
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

        // ScriptParser.LoadTexture
        [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.LoadTexture))]
        public static class LoadTexture_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(ScriptParser __instance, string resourceName)
            {
                Logger.LogInfo($"ScriptParser.LoadTexture called (resourceName: {resourceName})");
            }
        }
    }
}
