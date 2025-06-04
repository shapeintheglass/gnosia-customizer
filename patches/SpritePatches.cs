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
using static resource.ResourceManager;

namespace GnosiaCustomizer
{
    internal class SpritePatches
    {
        internal static new ManualLogSource Logger;

        // Individual sprites for each character, indexed by character folder name and head name
        private static Dictionary<string, Dictionary<string, Texture2D>> CharaSprites = [];

        // Keeping track of which sprites we have modified
        private static HashSet<uint> ModifiedSpriteIndeces = [];
        // Cached Sprite objects, generated during gameplay
        private static Dictionary<string, Sprite> SpriteCache = [];
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

        public struct CharaFileInfo
        {
            public bool hasCustomTexture;
            public Vector2[] sizes;
            public string[] headNamesNoExt;
            public byte[][] bytes;
        }

        internal static void Initialize()
        {
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

            // Unity libraries are not thread-safe and must be executed synchronously
            CreateTextureReplacements(filePathToBytesMap);
            CreateSpriteReplacements(filePathToBytesMap);
            Logger.LogInfo($"Loaded sprites for {CharaSprites.Keys.Count}/{Consts.CharaFolderNames.Length} characters.");
        }

        private static ConcurrentDictionary<string, byte[]> LoadTexturesAsynchronously(HashSet<string> textureFilePaths)
        {
            var filePathToBytesMap = new ConcurrentDictionary<string, byte[]>();
            var tasks = new List<Task>();

            Parallel.ForEach(textureFilePaths, new ParallelOptions { MaxDegreeOfParallelism = 8 }, file =>
            {
                filePathToBytesMap[Path.GetFullPath(file)] = File.ReadAllBytes(file);
            });

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
            Logger.LogInfo($"Loaded {filePathToBytesMap.Keys.Count} character textures.");
            return filePathToBytesMap;
        }

        private static void CreateTextureReplacements(ConcurrentDictionary<string, byte[]> filePathToBytesMap)
        {
            foreach (var filepath in filePathToBytesMap.Keys)
            {
                var newTexture = new Texture2D(2, 2);
                if (newTexture.LoadImage(filePathToBytesMap[filepath]))
                {
                    ReplacementTextures[Path.GetFileNameWithoutExtension(filepath)] = new ResourceManager.ResTextureList()
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

        public static void CreateSpriteReplacements(
            ConcurrentDictionary<string, byte[]> filePathToBytesMap)
        {
            var numTextures = Consts.HeadFileNamesWithExt.Length;
            var spriteNameAndBytes = new CharaFileInfo[Consts.CharaFolderNames.Length];

            for (var charaIndex = 0; charaIndex < Consts.CharaFolderNames.Length; charaIndex++)
            {
                var charaFolder = Consts.CharaFolderNames[charaIndex];

                if (!LoadHeadsForCharacter(numTextures, filePathToBytesMap, charaFolder,
                    out Vector2[] sizes, out byte[][] bytes, out string[] headNames))
                {
                    Logger.LogInfo("Skipping character " + charaFolder + " due to missing textures.");
                    continue;
                }

                spriteNameAndBytes[charaIndex] = new CharaFileInfo
                {
                    hasCustomTexture = true,
                    sizes = sizes,
                    bytes = bytes,
                    headNamesNoExt = headNames
                };
            }
            for (int i = 0; i < spriteNameAndBytes.Length; i++)
            {
                var charaIndex = i + 1;
                if (spriteNameAndBytes[i].hasCustomTexture)
                {
                    GenerateTextureForCharacter(Consts.CharaFolderNames[i], charaIndex, spriteNameAndBytes[i]);
                    Logger.LogInfo("Loaded custom textures for character: " + Consts.CharaFolderNames[i]);
                }
            }
        }

        private static bool LoadHeadsForCharacter(int numTextures,
            ConcurrentDictionary<string, byte[]> filePathToBytesMap,
            string charaFolder, out Vector2[] sizes, out byte[][] bytes, out string[] fileNames)
        {
            sizes = new Vector2[numTextures];
            bytes = new byte[numTextures][];
            fileNames = new string[numTextures];
            for (int textureIndex = 0; textureIndex < Consts.HeadFileNamesWithExt.Length; textureIndex++)
            {
                var headFileWithExt = Consts.HeadFileNamesWithExt[textureIndex];
                var headFilePath = Path.GetFullPath(Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder, headFileWithExt));
                if (!filePathToBytesMap.ContainsKey(headFilePath))
                {
                    Logger.LogInfo($"Custom sprite {headFilePath} not found in {charaFolder}. Valid keys: {filePathToBytesMap.Keys.Join()}.");
                    return false;
                }
                var headFileNameNoExt = Path.GetFileNameWithoutExtension(headFileWithExt);
                fileNames[textureIndex] = headFileNameNoExt;
                bytes[textureIndex] = filePathToBytesMap[headFilePath];
            }
            return true;
        }

        private static void GenerateTextureForCharacter(string charaFolderName, int absoluteId, CharaFileInfo info)
        {
            var numTextures = info.sizes.Length;

            for (int headIndex = 0; headIndex < numTextures; headIndex++)
            {
                var texture = new Texture2D(2, 2);
                if (!texture.LoadImage(info.bytes[headIndex]))
                {
                    Logger.LogError($"Failed to load texture {info.headNamesNoExt[headIndex]} for chara index {absoluteId}");
                    return;
                }

                if (!CharaSprites.TryGetValue(charaFolderName, out var headSpriteDict))
                {
                    headSpriteDict = new Dictionary<string, Texture2D>();
                    CharaSprites[charaFolderName] = headSpriteDict;
                }

                headSpriteDict[Consts.HeadNames[headIndex]] = texture;
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

                for (uint charIndex = 0; charIndex < Consts.CharaFolderNames.Length; charIndex++)
                {
                    var packedName = Consts.CharaFolderNames[charIndex];
                    var headOffset = 0;
                    for (int headIndex = 0; headIndex < Consts.HeadNames.Length; headIndex++)
                    {
                        var headName = Consts.HeadNames[headIndex];

                        if (!CharaSprites.TryGetValue(packedName, out Dictionary<string, Texture2D> value)
                            || !value.TryGetValue(headName, out var texture))
                        {
                            Logger.LogWarning($"Custom sprite {packedName} not found in {CharaSprites.Keys.Join()}. Skipping.");
                            continue;
                        }

                        var spriteIndex = Consts.CharSpriteIndeces[charIndex] + (uint) headOffset++;
                        var absoluteId = charIndex + 1;
                        var position = new Vector2(50f * absoluteId - 200f, 0f);
                        SetPackedTextureWithCache(
                            __instance,
                            resourceManager,
                            __instance.transform,
                            packedName,
                            headName,
                            spriteIndex,
                            Consts.Order,
                            position,
                            texture,
                            defaultMat,
                            colorCoeff,
                            displayHeight,
                            textureRatio);
                        ModifiedSpriteIndeces.Add(spriteIndex);
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
                Texture2D texture,
                Material mat,
                Color colorCoeff,
                float displayHeight,
                float textureRatio)
        {
            Logger.LogInfo($"SetPackedTextureWithCache called (packedName: {packedName}, textureName: {textureName}, depth: {depth}, order: {order}, _position: {_position})");

            var textureDimensions = new Vector2(texture.width, texture.height);

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
            sprite.m_texture = new ResTextureList
            {
                count = 1,
                isFixed = false,
                slot = 0,
                userInfo = new GraphicsContext.TextureUserInfo
                {
                    size = new Vector2(texture.width, texture.height),
                    isMadeInGame = false
                },
                texture = texture
            }; ;
            DisplayOffsetField.SetValue(sprite, _position);
            var displayOffsetVec = new Vector2(_position.x / 3f * 4f, _position.y / 3f * 4f * -1f);
            DisplayOffsetObjField.SetValue(sprite, displayOffsetVec);
            SizeInDisplayField.SetValue(sprite, textureDimensions);
            TextureOffsetField.SetValue(sprite, Vector2.zero);
            SizeInTextureField.SetValue(sprite, textureDimensions);

            // Game object image
            var image = gameObject.AddComponent<Image>();
            image.material = mat;
            image.material.SetColor("_Color", colorCoeff);
            var cachedName = $"{packedName}_{textureName}";
            if (!SpriteCache.TryGetValue(cachedName, out var cachedSprite))
            {
                cachedSprite = Sprite.Create(texture, new Rect(Vector2.zero, textureDimensions), Consts.ZeroOne);
                SpriteCache[cachedName] = cachedSprite;
            }
            image.sprite = cachedSprite;
            image.rectTransform.sizeDelta = textureDimensions;
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
