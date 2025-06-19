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
using coreSystem;
using gnosia;
using GnosiaCustomizer.utils;
using HarmonyLib;
using resource;
using UnityEngine;
using UnityEngine.UI;
using static resource.ResourceManager;

namespace GnosiaCustomizer
{
    internal class SpritePatches
    {
        internal static ManualLogSource Logger;

        // Available sprites for each character, indexed by character folder name
        private static Dictionary<string, List<int>> AvailableCharacterSprites = [];
        // Individual sprites for each character, indexed by character folder name and head name
        private static Dictionary<string, Dictionary<int, Lazy<CharaSpriteInfo>>> CharaSprites = [];

        // Keeping track of which sprites we have modified
        private static HashSet<uint> LoadedSpriteIndeces = [];
        private static HashSet<uint> ModifiedSpriteIndeces = [];
        // Currently visible sprite indeces in the CharaScreen
        private static HashSet<uint> VisibleSprites = [];
        // Replacement textures loaded from file
        private static Dictionary<string, Lazy<ResTextureList>> ReplacementTextures = [];

        // Reflection consts
        private static readonly Type Sprite2dEffectArgType = typeof(Sprite2dEffectArg);
        private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo DisplayOffsetField = Sprite2dEffectArgType.GetField("m_displayOffset", PrivateInstance);
        private static readonly FieldInfo DisplayOffsetObjField = Sprite2dEffectArgType.GetField("m_displayOffsetObj", PrivateInstance);
        private static readonly FieldInfo SizeInDisplayField = Sprite2dEffectArgType.GetField("m_sizeInDisplay", PrivateInstance);
        private static readonly FieldInfo TextureOffsetField = Sprite2dEffectArgType.GetField("m_textureOffset", PrivateInstance);
        private static readonly FieldInfo SizeInTextureField = Sprite2dEffectArgType.GetField("m_sizeInTexture", PrivateInstance);
        private static readonly FieldInfo ImageField = Sprite2dEffectArgType.GetField("m_image", PrivateInstance);

        public struct CharaSpriteInfo
        {
            public Sprite sprite;
            public Texture2D texture;
            public Vector2 size;
        }

        internal static void Initialize()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var texturesPath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, Consts.TextureAssetsFolder);

            var textureFilePaths = new HashSet<string>();
            if (Directory.Exists(texturesPath))
            {
                var files = Directory.GetFiles(texturesPath, "*.png", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    textureFilePaths.Add(file);
                }
            }

            // Also add textures in the character folders
            var absoluteId = 1;
            foreach (var charaFolder in Consts.CharaFolderNames)
            {
                var charaPath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder);
                if (Directory.Exists(charaPath))
                {
                    AvailableCharacterSprites[charaFolder] = [];
                    // Locate all files in this directory with the format h\d\d.png
                    var headFiles = Directory.GetFiles(charaPath, "h??.png", SearchOption.TopDirectoryOnly);

                    foreach (var headFilePath in headFiles)
                    {
                        var headFileNameWithoutExt = Path.GetFileNameWithoutExtension(headFilePath);
                        // Verify that the second two characters are digits
                        if (headFileNameWithoutExt.Length == 3 && char.IsDigit(headFileNameWithoutExt[1]) && char.IsDigit(headFileNameWithoutExt[2]))
                        {
                            var headIndex = int.Parse(headFileNameWithoutExt.Substring(1, 2));
                            if (headIndex >= 0 && headIndex < Consts.MaxNumHeads)
                            {
                                textureFilePaths.Add(headFilePath);
                                AvailableCharacterSprites[charaFolder].Add(headIndex);

                                // Calculate the corresponding sprite index
                                var spriteIndex = (absoluteId * 100) + headIndex;
                                ModifiedSpriteIndeces.Add((uint)spriteIndex);
                            }
                        }
                    }
                }
                absoluteId++;
            }

            if (textureFilePaths.Count == 0)
            {
                Logger.LogInfo("No texture files found in the assets folder or character folders.");
                return;
            }

            // We can asynchronously load the bytes from file
            var filePathToBytesMap = LoadTexturesAsynchronously(textureFilePaths);
            Logger.LogInfo($"Loaded {filePathToBytesMap.Keys.Count} texture files in {sw.ElapsedMilliseconds} ms.");
            sw.Restart();
            // Unity libraries are not thread-safe and must be executed synchronously.
            // We include lazy loading so that the sprites are only initialized the first time they are used.
            CreateTextureReplacements(filePathToBytesMap);
            Logger.LogInfo($"Created texture replacements in {sw.ElapsedMilliseconds} ms.");
            sw.Restart();
            CreateSpriteReplacements(filePathToBytesMap);
            Logger.LogInfo($"Loaded sprites for {CharaSprites.Keys.Count}/{Consts.CharaFolderNames.Length} characters in {sw.ElapsedMilliseconds} ms.");
        }

        // Loads all character and non-character textures from file asynchronously and caches their bytes in a ConcurrentDictionary.
        private static ConcurrentDictionary<string, byte[]> LoadTexturesAsynchronously(HashSet<string> textureFilePaths)
        {
            var filePathToBytesMap = new ConcurrentDictionary<string, byte[]>();

            Parallel.ForEach(textureFilePaths, new ParallelOptions { MaxDegreeOfParallelism = 8 }, file =>
            {
                filePathToBytesMap[Path.GetFullPath(file)] = File.ReadAllBytes(file);
            });

            return filePathToBytesMap;
        }

        // Processes non-character sprites and stores them as ResTextureList.
        private static void CreateTextureReplacements(ConcurrentDictionary<string, byte[]> filePathToBytesMap)
        {
            foreach (var filepath in filePathToBytesMap.Keys)
            {
                // Utilize lazy loading so that we don't create the textures until they're actually needed.
                ReplacementTextures[Path.GetFileNameWithoutExtension(filepath)] = new Lazy<ResTextureList>(() =>
                {
                    var newTexture = new Texture2D(2, 2);

                    if (newTexture.LoadImage(filePathToBytesMap[filepath]))
                    {
                        return new ResTextureList()
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
                        Logger.LogError($"Failed to load background texture for {filepath}");
                        return default;
                    }
                });
            }
        }

        // Processes character sprites and caches them as Sprite and Texture objects.
        public static void CreateSpriteReplacements(ConcurrentDictionary<string, byte[]> filePathToBytesMap)
        {
            for (var charaIndex = 0; charaIndex < Consts.NumCharacters; charaIndex++)
            {
                var charaFolder = Consts.CharaFolderNames[charaIndex];

                CharaSprites[charaFolder] = [];
                if (!AvailableCharacterSprites.TryGetValue(charaFolder, out var headIndeces))
                {
                    Logger.LogWarning($"No available sprites found for character {charaFolder}.");
                    continue;
                }

                foreach (var headIndex in headIndeces)
                {
                    var headFilePath = Path.GetFullPath(Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder, $"h{headIndex:D2}.png"));

                    if (!filePathToBytesMap.TryGetValue(headFilePath, out var bytes))
                    {
                        Logger.LogInfo($"Unable to load bytes for sprite {headFilePath} in {charaFolder}.");
                        Logger.LogInfo($"Available keys: {filePathToBytesMap.Keys.Join()}");
                        continue;
                    }

                    CharaSprites[charaFolder][headIndex] = new Lazy<CharaSpriteInfo>(() =>
                    {

                        var texture = new Texture2D(2, 2);
                        if (!texture.LoadImage(bytes))
                        {
                            Logger.LogError($"Unable to create texture {headIndex} from {charaFolder}.");
                            return default;
                        }
                        var textureDimensions = new Vector2(texture.width, texture.height);

                        return new CharaSpriteInfo
                        {
                            sprite = Sprite.Create(texture, new Rect(Vector2.zero, textureDimensions), Consts.ZeroOne),
                            texture = texture,
                            size = textureDimensions
                        };
                    });
                }
            }
        }

        [HarmonyPatch(typeof(CharaScreen), nameof(CharaScreen.InitializeGlm))]
        public static class CharaScreen_InitializeGlm_Patch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // Reset modified sprite indeces
                LoadedSpriteIndeces = [];
            }
        }

        private static bool SetPackedTextureWithCache(
            application.Screen __instance,
            ResourceManager resourceManager,
            uint spriteIndex,
            out Sprite2dEffectArg sprite)
        {
            // Assert required reflection fields are available
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

            // Determine the character ID from the sprite index
            var absoluteId = spriteIndex / 100;
            var headIndex = (int) spriteIndex % 100;

            var packedName = Consts.CharaFolderNames[absoluteId - 1];

            Lazy<CharaSpriteInfo> lazySpriteInfo = null;
            if (!CharaSprites.TryGetValue(packedName, out var value)
                || !value.TryGetValue(headIndex, out lazySpriteInfo))
            {
                // Try to fall back to head index 0
                if (headIndex != 0 && value.TryGetValue(0, out lazySpriteInfo))
                {
                    Logger.LogWarning($"Custom sprite {packedName} with head index {headIndex} not found. Falling back to head index 0.");
                }
                else
                {
                    Logger.LogWarning($"Custom sprite {packedName} with head index {headIndex} not found in {CharaSprites.Keys.Join()}. Skipping.");
                    sprite = null;
                    return false;
                }
            }

            var spriteInfo = lazySpriteInfo.Value;
            var position = new Vector2(50f * absoluteId - 200f, 0f);

            var parentTrans = __instance.transform;
            var colorCoeff = (Color)__instance.GetColorCoeff();
            var textureRatio = GraphicsContext.m_textureRatio;
            var displayHeight = resourceManager.m_displaySize.height;
            var mat = resourceManager.uiCharaDefaultMat;

            GameObject gameObject = new GameObject();
            gameObject.transform.SetParent(parentTrans);
            gameObject.SetActive(false);

            // Game object sprite
            sprite = gameObject.AddComponent<Sprite2dEffectArg>();
            sprite.m_type = 0;
            sprite.m_texture = new ResTextureList
            {
                count = 1,
                isFixed = false,
                slot = 0,
                userInfo = new GraphicsContext.TextureUserInfo
                {
                    size = spriteInfo.size,
                    isMadeInGame = false
                },
                texture = spriteInfo.texture
            };
            DisplayOffsetField.SetValue(sprite, position);
            var displayOffsetVec = new Vector2(position.x / 3f * 4f, position.y / 3f * 4f * -1f);
            DisplayOffsetObjField.SetValue(sprite, displayOffsetVec);
            SizeInDisplayField.SetValue(sprite, spriteInfo.size);
            TextureOffsetField.SetValue(sprite, Vector2.zero);
            SizeInTextureField.SetValue(sprite, spriteInfo.size);

            // Game object image
            var image = gameObject.AddComponent<Image>();
            image.material = mat;
            image.material.SetColor("_Color", colorCoeff);            
            image.sprite = spriteInfo.sprite;
            image.rectTransform.sizeDelta = spriteInfo.size;
            image.rectTransform.anchorMax = Consts.ZeroOne;
            image.rectTransform.anchorMin = Consts.ZeroOne;
            image.rectTransform.pivot = Consts.ZeroOne;
            image.rectTransform.anchoredPosition3D = (Vector3)displayOffsetVec;
            image.rectTransform.localScale = Vector3.one;
            ImageField.SetValue(sprite, image);

            sprite.SetSize(0.7f);
            sprite.SetDisplayOffsetY(displayHeight - sprite.GetSizeInDisplay().y * sprite.GetSize() * textureRatio);
            return true;
        }

        [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetTexture))]
        public static class GetTexture_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(string resourceName, ref ResTextureList __result)
            {
                //Logger.LogInfo($"GetTexture_Patch.Prefix called (resourceName: {resourceName})");
                if (ReplacementTextures.TryGetValue(resourceName, out var resTextureList))
                {
                    __result = resTextureList.Value;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.ShowChara))]
        public static class ShowChara_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(ScriptParser __instance, int chara, int hyojo, int pos = 0, uint depth = 20, bool charaisId = false)
            {
                if (chara <= 0)
                {
                    return false;
                }
                // We need to calculate the sprite index to determine if this should be a custom sprite.
                int thyojo = hyojo % 100;
                var gameData = Utils.GetGameDataViaReflection();
                if (gameData == null)
                {
                    Logger.LogWarning("Failed to get GameData");
                    return true;
                }
                int tid = charaisId ? chara : (int)gameData.chara[chara].id;

                var spriteIndex = (uint)tid * 100U;
                if (thyojo > 0)
                {
                    spriteIndex += (uint)thyojo;
                }
                // Skip if this is not a customized sprite
                if (!ModifiedSpriteIndeces.Contains(spriteIndex))
                {
                    Logger.LogInfo($"Sprite index {spriteIndex} is not modified, skipping.");
                    VisibleSprites.Add(spriteIndex);
                    return true;
                }

                // For custom sprites, do not draw the default sprite underneath
                __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                {
                    // Make sure the screen is valid
                    if (!__instance.m_sb.TryGetValue(depth, out var screen))
                    {
                        Logger.LogWarning($"Screen with depth {depth} not found in script parser.");
                        return true;
                    }

                    screen.m_spriteMap.TryGetValue(spriteIndex, out var sprite);

                    // If we haven't swapped out the sprite yet in this screen, try to update it
                    if (!LoadedSpriteIndeces.Contains(spriteIndex))
                    {
                        Logger.LogInfo($"Loading modified sprite index {spriteIndex}");
                        if (SetPackedTextureWithCache(screen, __instance.m_rs, spriteIndex, out sprite))
                        {
                            screen.m_spriteMap[spriteIndex] = sprite;
                        }
                        LoadedSpriteIndeces.Add(spriteIndex);
                    }

                    if (sprite == null)
                    {
                        Logger.LogWarning($"Sprite for index {spriteIndex} not found in screen with depth {depth}.");
                        return true;
                    }

                    // Position the sprite appropriately
                    var centerX = (__instance.m_rs.m_displaySize.width / 4 * (pos + 1)) + sprite.m_faceCenter * sprite.GetSize();
                    var centerY = sprite.GetCenterPosition().y;
                    sprite.SetCenterPosition(new Vector2(centerX, centerY));
                    sprite.SetVisible(true);
                    VisibleSprites.Add(spriteIndex);
                    return true;
                }), (ScriptParser.Script._EndFunc)(e => true), false));
                return false;
            }
        }

        [HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.UnvisibleAllChara))]
        public static class UnvisibleAllChara_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(ScriptParser __instance, ref int __result, uint depth = 20, int chara = -1)
            {
                if (chara <= 0)
                    __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                    {
                        foreach (var spriteIndex in VisibleSprites)
                        {
                            if (__instance.m_sb[depth].m_spriteMap.ContainsKey(spriteIndex))
                            {
                                __instance.m_sb[depth].m_spriteMap[spriteIndex].UnvisibleWithChild();
                            }
                        }

                        // Also iterate through all base sprites
                        for (var absoluteId = 1; absoluteId < Consts.NumCharacters + 1; absoluteId++)
                        {
                            var spriteIndex = (uint) absoluteId * 100U;
                            for (uint headIndex = 0; headIndex < 8U; ++headIndex)
                            {
                                var headSpriteIndex = spriteIndex + headIndex;
                                if (__instance.m_sb[depth].m_spriteMap.ContainsKey(headSpriteIndex))
                                {
                                    __instance.m_sb[depth].m_spriteMap[headSpriteIndex].UnvisibleWithChild();
                                }
                            }
                        }
                        VisibleSprites.Clear();
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
                        // Also iterate through every head
                        for (uint headIndex = 0; headIndex < Consts.MaxNumHeads; ++headIndex)
                        {
                            var headSpriteIndex = spriteIndex + headIndex;
                            if (__instance.m_sb[depth].m_spriteMap.ContainsKey(headSpriteIndex))
                            {
                                __instance.m_sb[depth].m_spriteMap[headSpriteIndex].UnvisibleWithChild();
                                VisibleSprites.Remove(headSpriteIndex);
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
                if (ReplacementTextures.TryGetValue(textureName, out var resTextureList))
                {
                    var display = _position ?? Vector2.zero;
                    var gameObject = new GameObject(textureName);
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
                    __instance.m_spriteMap[depth].SetFromLeftUpper(textureType, display, resTextureList.Value, resourceManager.uiDefaultMat);
                    __result = 1;
                    return false;
                }
                return true;
            }
        }

        //[HarmonyPatch(typeof(ScriptParser), nameof(ScriptParser.LoadTexture))]
        public static class LoadTexture_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(ScriptParser __instance, string resourceName)
            {
                Logger.LogInfo($"ScriptParser.LoadTexture called (resourceName: {resourceName})");
            }
        }

        //[HarmonyPatch(typeof(ScriptParser), "_SetScreen")]
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
