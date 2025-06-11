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
using GnosiaCustomizer.utils;
using HarmonyLib;
using resource;
using systemService.trophy;
using UnityEngine;
using UnityEngine.UI;
using static resource.ResourceManager;

namespace GnosiaCustomizer
{
    internal class SpritePatches
    {
        internal static ManualLogSource Logger;

        // Individual sprites for each character, indexed by character folder name and head name
        private static Dictionary<string, Dictionary<string, CharaSpriteInfo>> CharaSprites = [];

        // Keeping track of which sprites we have modified
        private static HashSet<uint> ModifiedSpriteIndeces = [];
        // Replacement textures loaded from file
        private static Dictionary<string, ResTextureList> ReplacementTextures = [];

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
            if (!Directory.Exists(texturesPath))
            {
                Logger.LogError($"Unable to locate custom textures folder {texturesPath}.");
                return;
            }

            // Locate all custom textures
            var textureFilePaths = new HashSet<string>(Directory.GetFiles(texturesPath, "*.png", SearchOption.TopDirectoryOnly));

            // Also add textures in the character folders
            foreach (var charaFolder in Consts.CharaFolderNames)
            {
                var allTexturesFound = true;
                var toAdd = new List<string>();
                var charaPath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder);
                if (Directory.Exists(charaPath))
                {
                    Logger.LogInfo($"Looking for custom character sprites in {charaPath}...");
                    foreach (var headFileName in Consts.HeadFileNamesWithExt)
                    {
                        var headFilePathWithExt = Path.Combine(charaPath, headFileName);
                        if (File.Exists(headFilePathWithExt))
                        {
                            toAdd.Add(headFilePathWithExt);
                        }
                        else
                        {
                            allTexturesFound = false;
                            break;
                        }
                    }
                }
                if (allTexturesFound)
                {
                    foreach (var file in toAdd)
                    {
                        textureFilePaths.Add(file);
                    }
                }
            }

            // We can asynchronously load the bytes from file
            var filePathToBytesMap = LoadTexturesAsynchronously(textureFilePaths);
            Logger.LogInfo($"Loaded {filePathToBytesMap.Keys.Count} texture files in {sw.ElapsedMilliseconds} ms.");
            sw.Restart();
            // Unity libraries are not thread-safe and must be executed synchronously
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
                var newTexture = new Texture2D(2, 2);
                if (newTexture.LoadImage(filePathToBytesMap[filepath]))
                {
                    ReplacementTextures[Path.GetFileNameWithoutExtension(filepath)] = new ResTextureList()
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
                }
            }
        }

        // Processes character sprites and caches them as Sprite and Texture objects.
        public static void CreateSpriteReplacements(ConcurrentDictionary<string, byte[]> filePathToBytesMap)
        {
            for (var charaIndex = 0; charaIndex < Consts.NumCharacters; charaIndex++)
            {
                var charaFolder = Consts.CharaFolderNames[charaIndex];

                CharaSprites[charaFolder] = [];

                for (int headIndex = 0; headIndex < Consts.NumHeads; headIndex++)
                {
                    var headFileWithExt = Consts.HeadFileNamesWithExt[headIndex];
                    var headFilePath = Path.GetFullPath(Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder, headFileWithExt));
                    if (!filePathToBytesMap.TryGetValue(headFilePath, out var bytes))
                    {
                        Logger.LogInfo($"Custom sprite {headFilePath} not found in {charaFolder}.");
                        continue;
                    }
                    var headFileNameNoExt = Path.GetFileNameWithoutExtension(headFileWithExt);

                    var texture = new Texture2D(2, 2);
                    if (!texture.LoadImage(bytes))
                    {
                        Logger.LogError($"Failed to load texture {headFileNameNoExt} in in folder {charaFolder}");
                        return;
                    }

                    var textureDimensions = new Vector2(texture.width, texture.height);
                    CharaSprites[charaFolder][Consts.HeadNames[headIndex]] = new CharaSpriteInfo
                    {
                        sprite = Sprite.Create(texture, new Rect(Vector2.zero, textureDimensions), Consts.ZeroOne),
                        texture = texture,
                        size = textureDimensions
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

                var displayHeight = resourceManager.m_displaySize.height;
                var textureRatio = GraphicsContext.m_textureRatio;
                var defaultMat = resourceManager.uiCharaDefaultMat;
                var colorCoeff = (Color)__instance.GetColorCoeff();

                for (uint charIndex = 0; charIndex < Consts.NumCharacters; charIndex++)
                {
                    var packedName = Consts.CharaFolderNames[charIndex];
                    var headOffset = 0;
                    for (int headIndex = 0; headIndex < Consts.NumHeads; headIndex++)
                    {
                        var headName = Consts.HeadNames[headIndex];

                        if (!CharaSprites.TryGetValue(packedName, out Dictionary<string, CharaSpriteInfo> value)
                            || !value.TryGetValue(headName, out var sprite))
                        {
                            Logger.LogWarning($"Custom sprite {packedName} not found in {CharaSprites.Keys.Join()}. Skipping.");
                            continue;
                        }

                        var spriteIndex = Consts.CharSpriteIndeces[charIndex] + (uint) headOffset++;
                        var absoluteId = charIndex + 1;
                        var position = new Vector2(50f * absoluteId - 200f, 0f);
                        SetPackedTextureWithCache(
                            __instance,
                            __instance.transform,
                            packedName,
                            headName,
                            spriteIndex,
                            Consts.Order,
                            position,
                            sprite,
                            defaultMat,
                            colorCoeff,
                            displayHeight,
                            textureRatio);
                        ModifiedSpriteIndeces.Add(spriteIndex);
                    }
                }
            }
        }

        private static void SetPackedTextureWithCache(
            application.Screen __instance,
            Transform parentTrans,
            string packedName,
            string textureName,
            uint depth,
            uint order,
            Vector2 position,
            CharaSpriteInfo spriteInfo,
            Material mat,
            Color colorCoeff,
            float displayHeight,
            float textureRatio)
        {
            Logger.LogInfo($"SetPackedTextureWithCache called (packedName: {packedName}, textureName: {textureName}, depth: {depth}, order: {order}, _position: {position})");

            GameObject gameObject = new GameObject(textureName);
            gameObject.transform.SetParent(parentTrans);
            gameObject.SetActive(false);

            // Game object sprite
            var sprite = gameObject.AddComponent<Sprite2dEffectArg>();
            __instance.m_spriteMap[depth] = sprite;
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
        }

        [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetTexture))]
        public static class GetTexture_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(string resourceName, ref ResTextureList __result)
            {
                if (ReplacementTextures.TryGetValue(resourceName, out var resTextureList))
                {
                    //Logger.LogInfo($"GetTexture_Patch.Prefix called (resourceName: {resourceName})");
                    __result = resTextureList;
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

                var spriteIndex = (uint)tid * 100U;
                if (thyojo > 0)
                {
                    spriteIndex += (uint)thyojo;
                }

                // For custom sprites, do not draw the default sprite underneath
                __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                {
                    if (thyojo > 0 && ModifiedSpriteIndeces.Contains(spriteIndex)
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
