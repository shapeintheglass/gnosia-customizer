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

        private static readonly uint[] charSpriteIndeces = [100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400];
        private static readonly Vector2 ZeroOne = new Vector2(0.0f, 1.0f);

        // Stores bytes of textures loaded from file
        private static ConcurrentDictionary<string, byte[]> filePathToBytesMap = new ConcurrentDictionary<string, byte[]>();

        // Character absolute IDs that have custom sprites
        private static HashSet<int> customSpriteAbsIds = new HashSet<int>();
        // Cached sprite sheet information
        private static Dictionary<string, CharaSpriteInfo> cachedSpriteSheets = new Dictionary<string, CharaSpriteInfo>();
        // Cached Sprite objects, generated during gameplay
        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        // Replacement textures loaded from file
        private static Dictionary<string, ResourceManager.ResTextureList> replacementTextures = new Dictionary<string, ResourceManager.ResTextureList>();

        // Reflection consts
        private static readonly Type Sprite2dEffectArgType = typeof(Sprite2dEffectArg);
        private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly FieldInfo DisplayOffsetField = Sprite2dEffectArgType.GetField("m_displayOffset", PrivateInstance);
        private static readonly FieldInfo DisplayOffsetObjField = Sprite2dEffectArgType.GetField("m_displayOffsetObj", PrivateInstance);
        private static readonly FieldInfo SizeInDisplayField = Sprite2dEffectArgType.GetField("m_sizeInDisplay", PrivateInstance);
        private static readonly FieldInfo TextureOffsetField = Sprite2dEffectArgType.GetField("m_textureOffset", PrivateInstance);
        private static readonly FieldInfo SizeInTextureField = Sprite2dEffectArgType.GetField("m_sizeInTexture", PrivateInstance);
        private static readonly FieldInfo ImageField = Sprite2dEffectArgType.GetField("m_image", PrivateInstance);

        private struct CharaSpriteInfo
        {
            public ResourceManager.ResTextureList texture;
            public Vector2[] sizes;
            public Vector2[] offsets;
            public Vector2 position;
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
            LoadTexturesAsynchronously(textureFilePaths);
            // Pre-determine which characters can be replaced based on the loaded sprites
            CheckCustomCharacterSprites();
        }

        private static void CheckCustomCharacterSprites()
        {
            var absoluteId = 1;
            foreach (var charaFolder in Consts.CharaFolderNames)
            {
                bool hasCustomSprites = true;
                foreach (var headFileName in Consts.HeadFileNamesWithExt)
                {
                    var headFilePathWithExt = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder, headFileName);
                    if (!filePathToBytesMap.ContainsKey(Path.GetFullPath(headFilePathWithExt)))
                    {
                        hasCustomSprites = false;
                        break;
                    }
                }
                if (hasCustomSprites)
                {
                    Logger.LogInfo($"Found custom character sprites for char ID {absoluteId}");
                    customSpriteAbsIds.Add(absoluteId);
                }
                absoluteId++;
            }
        }

        private static void LoadTexturesAsynchronously(HashSet<string> textureFilePaths)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tasks = new List<Task>();

            foreach (var file in textureFilePaths)
            {
                tasks.Add(Task.Run(() =>
                {
                    
                    filePathToBytesMap[Path.GetFullPath(file)] = File.ReadAllBytes(file);
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
            Logger.LogInfo($"Loaded {filePathToBytesMap.Keys.Count} textures (including character sprites) in {sw.ElapsedMilliseconds} ms.");
        }

        private static bool CreateTextureForResourceName(string resourceName, out ResourceManager.ResTextureList resTextureList)
        {
            var filePath = Path.GetFullPath(Path.Combine(Paths.PluginPath, Consts.AssetsFolder, Consts.TextureAssetsFolder, resourceName + ".png"));
            resTextureList = null;
            if (filePathToBytesMap.TryGetValue(filePath, out var bytes))
            {
                var newTexture = new Texture2D(2, 2);
                if (newTexture.LoadImage(filePathToBytesMap[filePath]))
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    resTextureList = new ResourceManager.ResTextureList()
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
                    replacementTextures[Path.GetFileNameWithoutExtension(filePath)] = resTextureList;
                    Logger.LogInfo($"Lazy loaded texture for {resourceName} in {sw.ElapsedMilliseconds} ms.");
                    return true;
                }
                else
                {
                    Logger.LogError($"Failed to load custom texture for {resourceName}");
                }
            }
            return false;
        }

        private static bool LoadCharacterSpritesInScreen(
            int absoluteId,
            application.Screen screen,
            float displayHeight,
            Material defaultMat)
        {
            Logger.LogInfo($"LLZR LazyLoadCharacterSprites called (absoluteId: {absoluteId})");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var order = 10U;
            var bodyDepth = (uint)(absoluteId * 100U);
            var textureRatio = GraphicsContext.m_textureRatio;
            var colorCoeff = (Color)screen.GetColorCoeff();
            var charaIndex = absoluteId - 1;
            var packedName = Consts.CharaFolderNames[charaIndex];
            var numTextures = Consts.HeadFileNamesWithExt.Length;

            if (!cachedSpriteSheets.TryGetValue(Consts.CharaFolderNames[charaIndex], out var spriteSheet))
            {
                if (!GenerateSpriteSheetForCharacter(charaIndex, packedName, out spriteSheet))
                {
                    Logger.LogInfo($"LLZR No custom sprite sheet found for {packedName}. Skipping character.");
                    return false;
                }
                else
                {
                    cachedSpriteSheets[packedName] = spriteSheet;
                    Logger.LogInfo($"LLZR Generated new sprite sheet for {packedName}.");
                }
            }

            var spriteIndex = charSpriteIndeces[charaIndex];
            // Body
            SetSpriteInScreenAtDepth(
                screen,
                packedName,
                "body",
                spriteIndex,
                order,
                spriteSheet.position,
                spriteSheet.texture,
                defaultMat,
                colorCoeff,
                displayHeight,
                textureRatio,
                new PackedTexture(spriteSheet.offsets[0], spriteSheet.sizes[0], [], 0.0f));

            // Heads
            for (int headIndex = 1; headIndex < Consts.HeadFileNamesWithExt.Length; headIndex++)
            {
                var headTextureName = Path.GetFileNameWithoutExtension(Consts.HeadFileNamesWithExt[headIndex]);
                var headSpriteIndex = (uint)(spriteIndex + headIndex);
                SetSpriteInScreenAtDepth(
                    screen,
                    packedName,
                    headTextureName,
                    headSpriteIndex,
                    order,
                    spriteSheet.position,
                    spriteSheet.texture,
                    defaultMat,
                    colorCoeff,
                    displayHeight,
                    textureRatio,
                    new PackedTexture(spriteSheet.offsets[headIndex], spriteSheet.sizes[headIndex], [], 0.0f));
            }
            Logger.LogInfo($"LLZR LazyLoadCharacterSprites completed for {packedName} in {sw.ElapsedMilliseconds} ms.");
            return true;
        }

        /// <summary>
        /// Generates a sprite sheet for a character from the textures pre-loaded from file.
        /// </summary>
        private static bool GenerateSpriteSheetForCharacter(int charaIndex, string charaFolder, out CharaSpriteInfo charaSpriteInfo)
        {
            Logger.LogInfo($"LLZR GenerateSpriteSheetForCharacter called (charaIndex: {charaIndex}, charaFolder: {charaFolder})");

            int numTexturesPerSheet = Consts.HeadFileNamesWithExt.Length;
            charaSpriteInfo = default;

            if (!LoadHeadsForCharacter(numTexturesPerSheet, filePathToBytesMap, charaFolder,
                out byte[][] bytes, out string[] headNames))
            {
                Logger.LogInfo("LLZR Skipping character " + charaFolder + " due to missing textures.");
                return false;
            }
            var sizes = new Vector2[numTexturesPerSheet];
            var textures = new Texture2D[numTexturesPerSheet];
            float totalWidth = 0;
            float maxHeight = 0;

            for (int i = 0; i < numTexturesPerSheet; i++)
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
                if (texture.LoadImage(bytes[i]))
                {
                    textures[i] = texture;
                    sizes[i] = new Vector2(texture.width, texture.height);
                    totalWidth += texture.width;
                    maxHeight = Mathf.Max(maxHeight, texture.height);
                }
                else
                {
                    Logger.LogError($"LLZR Failed to load texture {headNames[i]} for {charaFolder}");
                    return false;
                }
            }

            // Create sprite sheet and fill it with transparency
            var spriteSheet = new Texture2D((int)totalWidth, (int)maxHeight, TextureFormat.RGBA32, mipChain: false);
            var clearArray = new Color32[spriteSheet.width * spriteSheet.height];
            for (int i = 0; i < clearArray.Length; i++)
            {
                clearArray[i] = new Color32(0, 0, 0, 0); // Fully transparent
            }
            spriteSheet.SetPixels32(clearArray);

            var offsets = new Vector2[numTexturesPerSheet];
            var currentX = 0f;

            for (int i = 0; i < textures.Length; i++)
            {
                var tex = textures[i];
                var size = sizes[i];
                offsets[i] = new Vector2(currentX, 0);
                currentX += size.x;
                spriteSheet.SetPixels32(
                    (int)offsets[i].x,
                    (int)offsets[i].y,
                    (int)size.x,
                    (int)size.y,
                    tex.GetPixels32()
                );
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

            charaSpriteInfo = new CharaSpriteInfo
            {
                texture = resourceList,
                sizes = sizes,
                offsets = offsets,
                position = new Vector2(50f * charaIndex - 200f, 0f)
            };
            return true;
        }

        /// <summary>
        /// Loads head textures for a character from the specified folder.
        /// </summary>
        private static bool LoadHeadsForCharacter(int numTextures,
            ConcurrentDictionary<string, byte[]> filePathToBytesMap,
            string charaFolder, out byte[][] bytes, out string[] fileNames)
        {
            Logger.LogInfo($"LLZR LoadHeadsForCharacter called (charaFolder: {charaFolder})");
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

        private static void SetSpriteInScreenAtDepth(
                application.Screen screen,
                string packedName,
                string textureName,
                uint depth,
                uint order,
                Vector2 position,
                ResourceManager.ResTextureList texture,
                Material mat,
                Color colorCoeff,
                float displayHeight,
                float textureRatio,
                PackedTexture textureConfig)
        {
            Logger.LogInfo($"LLZR SetSpriteInScreenAtDepth called (packedName: {packedName}, textureName: {textureName}, depth: {depth})");

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
            gameObject.transform.SetParent(screen.transform);
            gameObject.SetActive(false);

            // Game object sprite
            var sprite = gameObject.AddComponent<Sprite2dEffectArg>();
            screen.m_spriteMap[depth] = sprite;
            sprite.m_type = 0;
            sprite.m_texture = texture;
            DisplayOffsetField.SetValue(sprite, position);
            var displayOffsetVec = new Vector2(position.x / 3f * 4f, position.y / 3f * 4f * -1f);
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
                    var absoluteId = charIndex + 1;
                    LoadCharacterSpritesInScreen((int)absoluteId, __instance, displayHeight, defaultMat);
                }
            }
        }

        [HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetTexture))]
        public static class GetTexture_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(string resourceName, ref ResourceManager.ResTextureList __result)
            {
                if (!replacementTextures.TryGetValue(resourceName, out var resTextureList)
                    && !CreateTextureForResourceName(resourceName, out resTextureList))
                {
                    Logger.LogInfo($"Unable to lazy load texture for {resourceName}");
                    replacementTextures[resourceName] = null;
                    return true;
                }

                if (resTextureList != null)
                {
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

                var spriteIndex = (uint) tid * 100U;
                if (thyojo > 0)
                {
                    spriteIndex += (uint)thyojo;
                }

                // For custom sprites, do not draw the default sprite underneath
                __instance.scriptQueue.Enqueue(new ScriptParser.Script((ScriptParser.Script._MainFunc)(e =>
                {
                    if (thyojo > 0 
                        && customSpriteAbsIds.Contains(tid)
                        && __instance.m_sb.TryGetValue(depth, out var screen) 
                        && screen.m_spriteMap.TryGetValue(spriteIndex, out var sprite))
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

                if (!replacementTextures.TryGetValue(textureName, out var resTextureList)
                    && !CreateTextureForResourceName(textureName, out resTextureList))
                {
                    Logger.LogWarning($"Failed to lazy load texture for resource name {textureName}. Returning true to skip further processing.");
                    replacementTextures[textureName] = null;
                    return true;
                }

                if (resTextureList == null)
                {
                    // We already tried to lazy load this texture and it failed
                    return true;
                }

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
