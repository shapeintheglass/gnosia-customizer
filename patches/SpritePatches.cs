using System;
using System.Collections.Concurrent;
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

namespace GnosiaCustomizer
{
    internal class SpritePatches
    {
        internal static new ManualLogSource Logger;

        private static readonly string[] packedNames = ["p01", "p02", "p03", "p04", "p05", "p06", "p07", "p08", "p09", "p10", "p11", "p12", "p13", "p14"];
        private static readonly uint[] charSpriteIndeces = [100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400];
        private static readonly string[] headNames = ["h01", "h02", "h03", "h04", "h05", "h06", "h07"];
        private static readonly uint[] headOffsetIndeces = [1, 2, 3, 4, 5, 6, 7];

        private static Dictionary<string, CharaTexture> charaTextures = new Dictionary<string, CharaTexture>();
        private static HashSet<uint> modifiedSpriteIndeces = new HashSet<uint>();
        private static Dictionary<string, ResourceManager.ResTextureList> replacementTextures = new Dictionary<string, ResourceManager.ResTextureList>();

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

        public struct CharaFileInfo
        {
            public bool hasCustomTexture;
            public Vector2[] sizes;
            public string[] fileNames;
            public byte[][] bytes;
        }

        private struct CharaTexture
        {
            public ResourceManager.ResTextureList texture;
            public Vector2[] sizes;
            public Vector2[] offsets;
            public Vector2? position;
        }

        // Load custom sprites from the "textures" folder
        internal static void InitializeAsync()
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

            // We can asynchronously load the bytes from file, but Unity libraries are not thread-safe
            var customTextures = LoadCustomTextures(availableFiles, texturesPath);
            LoadReplacementTextures(customTextures);
            var spriteNameAndBytes = LoadCharacterTextures(customTextures);
            for (int i = 0; i < spriteNameAndBytes.Length; i++)
            {
                var charaIndex = i + 1;
                if (spriteNameAndBytes[i].hasCustomTexture)
                {
                    CreateTextures(packedNames[i], charaIndex, spriteNameAndBytes[i]);
                    Logger.LogInfo("Loaded custom textures for character: " + packedNames[i]);
                }
            }
        }

        private static ConcurrentDictionary<string, byte[]> LoadCustomTextures(HashSet<string> availableFiles, string texturesPath)
        {
            var customTextures = new ConcurrentDictionary<string, byte[]>();
            var tasks = new List<Task>();

            foreach (var file in availableFiles)
            {
                tasks.Add(Task.Run(() =>
                {
                    customTextures[file] = File.ReadAllBytes(Path.Combine(texturesPath, file));
                }));
            }

            try
            {
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                {
                    Logger.LogError($"Error loading texture: {inner.Message}");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Unexpected error loading textures: {e.Message}");
            }
            Logger.LogInfo($"Loaded {customTextures.Count} custom textures from {texturesPath}");
            return customTextures;
        }

        private static void LoadReplacementTextures(ConcurrentDictionary<string, byte[]> customTextures)
        {
            foreach (var file in customTextures.Keys)
            {
                // Skip textures that match the regex pXX_hYY.png
                if (System.Text.RegularExpressions.Regex.IsMatch(file, @"^p\d{2}_h\d{2}\.png$"))
                {
                    continue;
                }

                var newTexture = new Texture2D(2, 2);
                if (newTexture.LoadImage(customTextures[file]))
                {
                    replacementTextures[Path.GetFileNameWithoutExtension(file)] = new ResourceManager.ResTextureList()
                    {
                        count = 1,
                        isFixed = false,
                        slot = 0,
                        userInfo = new GraphicsContext.TextureUserInfo()
                        {
                            size = new Vector2(newTexture.width, newTexture.height),
                            isMadeInGame = false
                        },
                        texture = newTexture
                    };
                }
                else
                {
                    Logger.LogError($"Failed to load background texture {file}");
                }
            }
        }

        public static CharaFileInfo[] LoadCharacterTextures(
            ConcurrentDictionary<string, byte[]> customTextures)
        {

            var numTextures = headNames.Length + 1;
            var spriteNameAndBytes = new CharaFileInfo[packedNames.Length];

            for (var charaIndex = 0; charaIndex < packedNames.Length; charaIndex++)
            {
                var localIndex = charaIndex; // Capture for closure
                var chara = packedNames[localIndex];

                var sizes = new Vector2[numTextures];
                var bytes = new byte[numTextures][];
                var fileNames = new string[numTextures];

                string bodyFile = $"{chara}_h00.png";
                if (!customTextures.ContainsKey(bodyFile))
                {
                    Logger.LogInfo($"Body file not found: {bodyFile}");
                    continue;
                }
                fileNames[0] = bodyFile;
                var bodyFilePath = Path.Combine(Paths.PluginPath, "textures", bodyFile);
                bytes[0] = customTextures[bodyFile];

                for (int textureIndex = 0; textureIndex < headNames.Length; textureIndex++)
                {
                    var head = headNames[textureIndex];
                    var headFile = $"{chara}_{head}.png";
                    if (!customTextures.ContainsKey(headFile))
                    {
                        Logger.LogInfo($"Head file not found: {headFile}");
                        continue;
                    }
                    var headFilePath = Path.Combine(Paths.PluginPath, "textures", headFile);
                    fileNames[textureIndex + 1] = headFile;
                    bytes[textureIndex + 1] = customTextures[headFile];
                }

                spriteNameAndBytes[localIndex] = new CharaFileInfo
                {
                    hasCustomTexture = true,
                    sizes = sizes,
                    bytes = bytes,
                    fileNames = fileNames
                };
            }

            return spriteNameAndBytes;
        }

        private static void CreateTextures(string chara, int charaIndex, CharaFileInfo info)
        {
            var numTextures = info.sizes.Length;
            var textures = new Texture2D[numTextures];

            float totalWidth = 0;
            float maxHeight = 0;
            for (int i = 0; i < numTextures; i++)
            {
                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(info.bytes[i]))
                {
                    textures[i] = texture;
                    info.sizes[i] = new Vector2(texture.width, texture.height);
                    totalWidth += texture.width;
                    maxHeight = Mathf.Max(maxHeight, texture.height);
                }
                else
                {
                    Logger.LogError($"Failed to load texture {info.fileNames[i]} for chara index {charaIndex}");
                    return;
                }
            }

            // Create sprite sheet and fill it with transparency
            var spriteSheet = new Texture2D((int)totalWidth, (int)maxHeight, TextureFormat.RGBA32, false);
            var fill = new Color[spriteSheet.width * spriteSheet.height];
            for (int i = 0; i < fill.Length; i++)
            {
                fill[i] = Color.clear;
            }
            spriteSheet.SetPixels(fill);

            var offsets = new Vector2[numTextures];
            var currentX = 0f;

            for (int i = 0; i < textures.Length; i++)
            {
                var tex = textures[i];
                var size = info.sizes[i];
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
                sizes = info.sizes,
                offsets = offsets,
                position = new Vector2?(new Vector2(50f * charaIndex - 200f, 0f))
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
                Logger.LogInfo($"CharaScreen.InitializeGlm called");
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
                            10U,
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
            Logger.LogInfo($"SetPackedTextureWithCache called (packedName: {packedName}, textureName: {textureName}, depth: {depth}, order: {order}, _position: {_position})");
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
            [HarmonyPostfix]
            public static void Postfix(ScriptParser __instance, ref int __result, int chara, int hyojo, int pos = 0, uint depth = 20, bool charaisId = false)
            {
                Logger.LogInfo($"ShowChara_Patch called (chara: {chara}, hyojo: {hyojo}, pos: {pos}, depth: {depth}, charaisId: {charaisId})");
                
                if (chara <= 0)
                {
                    return;
                }
                // We need to calculate the sprite index to determine if this should be a custom sprite.
                int thyojo = hyojo % 100;
                var gameData = Utils.GetGameDataViaReflection();
                if (gameData == null)
                {
                    Logger.LogWarning("Failed to get GameData");
                    return;
                }
                int tid = charaisId ? chara : (int)gameData.chara[chara].id;

                var spriteIndex = (uint) tid * 100U;
                if (thyojo > 0)
                {
                    spriteIndex += (uint)thyojo;
                }

                // For custom sprites, do not draw the default sprite underneath
                __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                {
                    if (thyojo > 0 && modifiedSpriteIndeces.Contains(spriteIndex)
                        && __instance.m_sb.TryGetValue(depth, out var screen) && screen.m_spriteMap.TryGetValue(spriteIndex, out var sprite))
                    {
                        var defaultSprite = __instance.m_sb[depth].m_spriteMap[(uint)tid * 100U];
                        var centerX = (__instance.m_rs.m_displaySize.width / 4 * (pos + 1)) + sprite.m_faceCenter * sprite.GetSize();
                        var centerY = sprite.GetCenterPosition().y;
                        defaultSprite.SetVisible(false);
                        sprite.SetCenterPosition(new Vector2(centerX, centerY));
                    }
                    return true;
                }), (ScriptParser.Script._EndFunc)(e => true), false));
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
                if (replacementTextures.TryGetValue(textureName, out var resTextureList))
                {
                    Vector2 display = _position ?? Vector2.zero;
                    GameObject gameObject = new GameObject(textureName);
                    gameObject.AddComponent<Sprite2dEffectArg>();
                    gameObject.AddComponent<Image>();
                    gameObject.transform.SetParent(parentTrans);
                    gameObject.SetActive(false);
                    __instance.m_spriteMap[depth] = gameObject.GetComponent<Sprite2dEffectArg>();
                    var resourceManager = Utils.GetResourceManagerViaReflection(__instance);
                    if (resourceManager == null)
                    {
                        Logger.LogWarning("Failed to get ResourceManager instance via reflection");
                        return true;
                    }
                    __instance.m_spriteMap[depth].SetFromLeftUpper(textureType, display, resTextureList, resourceManager.uiDefaultMat);
                    __result = 1;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.LoadTexture))]
        public static class LoadTexture_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(ScriptParser __instance, string resourceName)
            {
                Logger.LogInfo($"ScriptParser.LoadTexture called (resourceName: {resourceName})");
            }
        }

        [HarmonyPatch(typeof(ScriptParser), "_SetScreen")]
        public static class SetScreen_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(ScriptParser __instance, application.Screen scr, uint depth, bool useGameLogManager = false)
            {
                Logger.LogInfo($"ScriptParser._SetScreen called (scr: {scr?.GetType().Name}, depth: {depth}, useGameLogManager: {useGameLogManager})");
            }
        }
    }
}
