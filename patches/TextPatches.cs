using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Linq;
using System;
using HarmonyLib;
using System.Reflection;
using GnosiaCustomizer.utils;
using gnosia;
using coreSystem;

namespace GnosiaCustomizer.patches
{
    internal class TextPatches
    {
        internal static new ManualLogSource Logger;

        private static readonly Dictionary<int, string> characterIdToFile = new Dictionary<int, string> {
            { 1, "chara01.yaml" },
            { 2, "chara02.yaml" },
            { 3, "chara03.yaml" },
            { 4, "chara04.yaml" },
            { 5, "chara05.yaml" },
            { 6, "chara06.yaml" },
            { 7, "chara07.yaml" },
            { 8, "chara08.yaml" },
            { 9, "chara09.yaml" },
            { 10, "chara10.yaml" },
            { 11, "chara11.yaml" },
            { 12, "chara12.yaml" },
            { 13, "chara13.yaml" },
            { 14, "chara14.yaml" },
        };

        private static Dictionary<int, CharacterText> characterTexts = new Dictionary<int, CharacterText>();

        internal static void Initialize()
        {
            Logger.LogInfo("LoadCustomText called");
            // Verify that the text folder exists
            string textFolderPath = Path.Combine(Paths.PluginPath, "text");
            if (!Directory.Exists(textFolderPath))
            {
                Logger.LogWarning($"Text folder not found at {textFolderPath}. No custom text will be loaded.");
                return;
            }

            // Get all available files in the directory
            var availableFiles = new HashSet<string>(
                Directory.GetFiles(textFolderPath, "*.yaml", SearchOption.TopDirectoryOnly)).Select(Path.GetFileName);

            foreach (var charaFile in characterIdToFile.Values)
            {
                if (!availableFiles.Contains(charaFile))
                {
                    Logger.LogInfo(charaFile + " not found in text folder. Skipping.");
                    continue;
                }
                // Load the character text file
                string yamlPath = Path.Combine(textFolderPath, charaFile);
                string yamlContent = File.ReadAllText(yamlPath);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

                try
                {
                    var character = deserializer.Deserialize<CharacterText>(yamlContent);
                    // Add the character text to the dictionary
                    int characterId = characterIdToFile.FirstOrDefault(x => x.Value == charaFile).Key;
                    
                    if (characterId != 0)
                    {
                        characterTexts[characterId] = character;
                        JinroPatches.SkillMap[characterId] = character.KnownSkills;
                        Logger.LogInfo($"Set skills for character ID {characterId}: {string.Join(", ", character.KnownSkills.Select(kv => $"{kv.Key}: {kv.Value}"))}");
                    }
                    else
                    {
                        Logger.LogWarning($"Character ID not found for file {charaFile}. Skipping.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to deserialize {charaFile}: {ex.Message}");
                    continue;
                }
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
                foreach (var absoluteId in characterIdToFile.Keys)
                {
                    if (characterTexts.TryGetValue(absoluteId, out var character) && !string.IsNullOrEmpty(character.Name))
                    {
                        CharacterSetter.SetChara(Logger, absoluteId, character);
                    }
                    // Log the new name
                    Logger.LogInfo($"Character ID {absoluteId} name is now: {CharacterSetter.GetCharaFieldValue(absoluteId, "name")}");
                    Logger.LogInfo($"Grovel lines are {CharacterSetter.GetCharaFieldValueAsStringArray(absoluteId, "t_skill_dogeza").Join()}");
                }
            }
        }

        [HarmonyPatch(typeof(ScriptParser), "SetNormalSerifu")]
        public class ScriptParserSetNormalSerifuPatch
        {
            static void Prefix(ScriptParser __instance, 
                int main,
                int tgt,
                int pos,
                List<string> lang,
                bool waitNextText = false,
                bool withoutTrans = false,
                bool withoutCharaChange = false,
                bool vRole = true)
            {
                Logger.LogInfo($"ScriptParser.SetNormalSerifu called with main={main}, tgt={tgt}, pos={pos}, lang={lang.Join()}, waitNextText={waitNextText}, " +
                    $"withoutTrans={withoutTrans}, withoutCharaChange={withoutCharaChange}, vrole={vRole}");
            }
        }

        // ScriptParser.SetText
        //[HarmonyPatch(typeof(ScriptParser), "SetText")]
        public class ScriptParserSetTextPatch
        {
            static void Prefix(ScriptParser __instance,
                ref string message, bool waitFinish = false, uint depth = 50, string targetText = "test")
            {
                Logger.LogInfo($"ScriptParser.SetText called with message='{message}', waitFinish={waitFinish}, depth={depth}, targetText='{targetText}'");
                if (string.IsNullOrEmpty(message))
                {
                    Logger.LogInfo("Using placeholder for empty message.");
                    message = "placeholder...";
                }
            }
        }
    }
}
