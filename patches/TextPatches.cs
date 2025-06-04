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
using coreSystem;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GnosiaCustomizer.patches
{
    internal class TextPatches
    {
        internal static new ManualLogSource Logger;
        private const string ConfigFileName = "config.yaml";
        private const string DialogueFileName = "dialogue.csv";

        private static ConcurrentDictionary<int, CharacterText> characterTexts = new ConcurrentDictionary<int, CharacterText>();
        private static ConcurrentDictionary<int, List<DialogueLine>> dialogueLines = new ConcurrentDictionary<int, List<DialogueLine>>();
        public class CsvRow
        {
            public string Name { get; set; }
            public string Index { get; set; }
            public string InnerIndex { get; set; }
            public string Desc { get; set; }
            public string Emotion { get; set; }
            public string Text { get; set; }
        }

        public struct DialogueLine
        {
            public string Name;
            public int? Index;
            public int? InnerIndex;
            public string Emotion;
            public string Text;
        }

        internal static void Initialize()
        {
            Logger.LogInfo("LoadCustomText called");

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
                    tasks.Add(Task.Run(() =>
                    {
                        string yamlContent = File.ReadAllText(yamlPath);
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(UnderscoredNamingConvention.Instance)
                            .Build();
                        try
                        {
                            var character = deserializer.Deserialize<CharacterText>(yamlContent);
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

                    }));
                }

                var csvPath = Path.Combine(charaPath, DialogueFileName);
                if (File.Exists(csvPath))
                {
                    tasks.Add(Task.Run(() => {
                        Logger.LogInfo($"Attempting to parse CSV for character {localCharacterId} at {csvPath}");

                        if (!dialogueLines.ContainsKey(localCharacterId))
                        {
                            dialogueLines[localCharacterId] = new List<DialogueLine>();
                        }

                        using var reader = new StreamReader(csvPath);
                        using var csv = new CsvHelper.CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(
                            System.Globalization.CultureInfo.InvariantCulture)
                        {
                            HasHeaderRecord = true,
                            IgnoreBlankLines = true,
                            BadDataFound = null
                        });
                        try
                        {
                            while (csv.Read())
                            {
                                CsvRow record = null;
                                try 
                                {
                                    record = csv.GetRecord<CsvRow>();
                                    dialogueLines[localCharacterId].Add(new DialogueLine
                                    {
                                        Name = record.Name,
                                        Index = int.TryParse(record.Index, out var idx) ? idx : (int?)null,
                                        InnerIndex = int.TryParse(record.InnerIndex, out var innerIdx) ? innerIdx : (int?)null,
                                        Emotion = record.Emotion,
                                        Text = record.Text
                                    });
                                } 
                                catch (Exception ex) 
                                {
                                    Logger.LogError($"Failed to parse CSV row at line {csv.Context.Parser.Row}: {ex.Message}");

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"CSV parsing failed for character {localCharacterId}: {ex.Message}");

                        }
                        Logger.LogInfo($"Parsed {dialogueLines[localCharacterId].Count} lines for character {localCharacterId}");
                    }));
                }
                else
                {
                    Logger.LogInfo($"No dialogue.csv found for character {localCharacterId} at {csvPath}");
                }
                characterId++;
            }

            try
            {
                Task.WhenAll(tasks).GetAwaiter().GetResult();
                Logger.LogInfo($"Loaded {characterTexts.Count}/{Consts.CharaFolderNames.Length} character configs");
                Logger.LogInfo($"Loaded {skillMap.Count}/{Consts.CharaFolderNames.Length} character skills");
                Logger.LogInfo($"Loaded {dialogueLines.Count}/{Consts.CharaFolderNames.Length} dialogue files");

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
                foreach (var absoluteId in Consts.CharaFolderIds)
                {
                    if (characterTexts.TryGetValue(absoluteId, out var character))
                    {
                        CharacterSetter.SetChara(Logger, absoluteId, character, dialogueLines[absoluteId]);
                    }
                    Logger.LogInfo($"Character ID {absoluteId} name is now: {CharacterSetter.GetCharaFieldValue(absoluteId, "name")}");

                    if (CharacterSetter.GetCharaFieldValueAsStringArray(absoluteId, "t_skill_dogeza", out var strArray))
                    {
                        Logger.LogInfo($"Dogeza test: Want 2 lines, got: {strArray.Count}");
                    }
                    else
                    {
                        Logger.LogInfo($"Dogeza test: No lines found for character {absoluteId}.");
                    }
                    //CharacterSetter.LogCharaFieldsToFile(absoluteId, Logger);
                }
                //CharacterSetter.LogCharaFieldsToFile(0, Logger);
            }
        }

        //[HarmonyPatch(typeof(ScriptParser), "SetNormalSerifu")]
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

        [HarmonyPatch(typeof(ScriptParser), "SetText")]
        public class ScriptParserSetTextPatch
        {
            static void Prefix(ScriptParser __instance,
                ref string message, bool waitFinish = false, uint depth = 50, string targetText = "test")
            {
                Logger.LogInfo($"ScriptParser.SetText called with message='{message}', waitFinish={waitFinish}, depth={depth}, targetText='{targetText}'");
                if (string.IsNullOrEmpty(message))
                {
                    Logger.LogInfo("Using placeholder for empty message.");
                    message = "...";
                }
            }
        }
    }
}
