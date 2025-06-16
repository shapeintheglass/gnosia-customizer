using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using System.Linq;
using System;
using HarmonyLib;
using System.Reflection;
using GnosiaCustomizer.utils;
using coreSystem;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GnosiaCustomizer.patches
{
    internal class TextPatches
    {
        internal static new ManualLogSource Logger;
        private const string ConfigFileName = "config.yaml";

        private static ConcurrentDictionary<int, CharacterText> characterTexts = [];

        private static Dictionary<string, string> NameReplacements = [];
        private static readonly List<string> NamesToReplace = [
            "Gina", "SQ", "Raqio", "Stella", "Shigemichi", "Chipie", "Remnan",
            "Comet", "Yuriko", "Jonas", "Setsu", "Otome", "Sha-Ming", "Kukrushka"
        ];

        internal static void Initialize()
        {
            Logger.LogInfo("LoadCustomText called");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            // Read from each character folder asynchronously
            var tasks = new List<Task>();
            int characterId = 1;
            var skillMap = new ConcurrentDictionary<int, Dictionary<string, bool>>();
            foreach (var charaFolder in Consts.CharaFolderNames)
            {
                var charaPath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder);
                if (!Directory.Exists(charaPath))
                {
                    Logger.LogInfo($"Could not find path {charaPath}");
                    characterId++;
                    continue;
                }

                var localCharacterId = characterId; // Capture the current characterId for the task
                var yamlPath = Path.Combine(charaPath, ConfigFileName);
                if (File.Exists(yamlPath))
                {
                    try
                    {
                        var character = new CharacterText();
                        character.LoadFromFile(yamlPath);
                        if (localCharacterId != 0)
                        {
                            characterTexts[localCharacterId] = character;
                            skillMap[localCharacterId] = character.KnownSkills;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to deserialize {charaFolder}: {ex.Message}");
                    }
                }
                characterId++;
            }

            try
            {
                Logger.LogInfo($"Loaded {characterTexts.Count}/{Consts.CharaFolderNames.Length} character configs");
                Logger.LogInfo($"Loaded {skillMap.Count}/{Consts.CharaFolderNames.Length} character skills");
                Logger.LogInfo($"LoadCustomText completed in {sw.ElapsedMilliseconds} ms");

                JinroPatches.SkillMap = skillMap.ToDictionary(kv => kv.Key, kv => kv.Value);
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
        }

        [HarmonyPatch]
        public class SetCharaDataPatch
        {
            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("gnosia.Data");
                return AccessTools.Method(type, "SetKukulsika"); // This is called last
            }

            static void Postfix()
            {
                // Uncomment to re-generate original config for the player
                //GenerateOriginalConfig.WriteCharaDataToFile(0);

                foreach (var absoluteId in Consts.CharaFolderIds)
                {
                    // Uncomment to re-generate the original character config
                    //GenerateOriginalConfig.WriteCharaDataToFile(absoluteId);

                    if (characterTexts.TryGetValue(absoluteId, out var character))
                    {
                        CharacterSetter.SetChara(Logger, absoluteId, character);
                    }
                    CharacterSetter.GetCharaFieldValueAsString(absoluteId, "name", out var name);
                    Logger.LogInfo($"Character ID {absoluteId} name is now: {name}");
                    NameReplacements[NamesToReplace[absoluteId-1]] = name;

                    if (CharacterSetter.GetCharaFieldValueAsStringArray(absoluteId, "t_skill_dogeza", out var strArray))
                    {
                        Logger.LogInfo($"Dogeza test: Want 2 lines, got: {strArray.Count}");
                    }
                    else
                    {
                        Logger.LogInfo($"Dogeza test: No lines found for character {absoluteId}.");
                    }
                    if (character.SingleLines.TryGetValue("night_friend_and_high_trust", out var personalLine))
                    {
                        Logger.LogInfo($"Personal lines test: {personalLine.Line}");
                    } else
                    {
                        Logger.LogInfo($"Personal lines test: No personal line found for character {absoluteId}.");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ScriptParser), "SetText")]
        public class ScriptParserSetTextPatch
        {
            static bool Prefix(ScriptParser __instance, ref string message)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "...";
                }
                else if (message.StartsWith(CharacterSetter.SubstitutionPrefix))
                {
                    var tokens = message.Split(CharacterSetter.Delimiter);
                    if (tokens.Length > 0)
                    {
                        // 0 - Prefix
                        // 1 - Character name
                        // 2 - Message name
                        // 3+ - Parameters
                        message = tokens[2];
                    }
                }
                else
                {
                    foreach (var name in NameReplacements.Keys)
                    {
                        if (message.Contains(name))
                        {
                            message = message.Replace(name, NameReplacements[name]);
                        }
                    }
                }
                return true;
            }
        }
    }
}
