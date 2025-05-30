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

        private static ConcurrentDictionary<int, CharacterText> characterTexts = new ConcurrentDictionary<int, CharacterText>();

        internal static void Initialize()
        {
            Logger.LogInfo("LoadCustomText called");

            // Read from each character folder asynchronously
            var tasks = new List<Task>();
            int characterId = 1;
            var skillMap = new ConcurrentDictionary<int, Dictionary<string, bool>>();
            foreach (var charaFolder in Consts.CharaFolderNames)
            {
                var charaPath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, charaFolder, ConfigFileName);
                if (!File.Exists(charaPath))
                {
                    characterId++;
                    continue;
                }

                var localCharacterId = characterId; // Capture the current characterId for the task
                tasks.Add(Task.Run(() =>
                {
                    string yamlContent = File.ReadAllText(charaPath);

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
                characterId++;
            }
            try
            {
                Task.WhenAll(tasks).GetAwaiter().GetResult();
                Logger.LogInfo($"Loaded {characterTexts.Count}/{Consts.CharaFolderNames.Length} character configs and initialized {skillMap.Count}/{Consts.CharaFolderNames.Length} character skills");
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
                    if (characterTexts.TryGetValue(absoluteId, out var character) && !string.IsNullOrEmpty(character.Name))
                    {
                        CharacterSetter.SetChara(Logger, absoluteId, character);
                    }
                    Logger.LogInfo($"Character ID {absoluteId} name is now: {CharacterSetter.GetCharaFieldValue(absoluteId, "name")}");
                }
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
