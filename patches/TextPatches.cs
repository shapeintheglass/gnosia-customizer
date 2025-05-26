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

namespace GnosiaCustomizer.patches
{
    internal class TextPatches : BaseUnityPlugin
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

        public class CharacterText
        {
            public string Name { get; set; }
            public int Sex { get; set; }
            public int Age { get; set; }
            public string Place { get; set; }
            public int NumJournalEntries { get; set; }
            public List<JournalEntry> JournalEntries { get; set; }
            public Dictionary<string, float> Attributes { get; set; }
            public Dictionary<string, float> AbilityStart { get; set; }
            public Dictionary<string, float> AbilityMax { get; set; }
            public Dictionary<string, bool> KnownSkills { get; set; }
            public Dictionary<string, bool> PreferredPlaces { get; set; }
            public Dictionary<string, bool> DislikedPlaces { get; set; }
            public int HpMin { get; set; }
            public int HpWithGnos { get; set; }
            public List<DialogueEntry> Dialogue { get; set; }
        }

        public class JournalEntry
        {
            public string Text { get; set; }
            public int Type { get; set; }
        }

        public class DialogueEntry
        {
            public string Type { get; set; }
            public List<Line> Lines { get; set; }
        }

        public class Line
        {
            public string Text { get; set; }
            public int? Face { get; set; } // Nullable int to handle cases where face is not present
        }

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


        // Patches

        // ScenarioEngineObj.Initialize()
        //[HarmonyPatch(typeof(gnosia.ScenarioEngineObj), nameof(gnosia.ScenarioEngineObj.Initialize))]
        //internal class ScenarioEngineObj_Initialize_Patch
        //{
        //    internal static void Postfix(gnosia.ScenarioEngineObj __instance)
        //    {
        //        Logger.LogInfo("ScenarioEngineObj.Initialize() called");
        //        foreach (var absoluteId in characterIdToFile.Keys)
        //        {
        //            if (characterTexts.TryGetValue(absoluteId, out var character) && !string.IsNullOrEmpty(character.Name))
        //            {
        //                utils.Utils.SetCharaFieldValue(absoluteId, "name", character.Name);
        //                Logger.LogInfo($"Set name for character ID {absoluteId} to {character.Name}");
        //            }
        //            // Log the new name
        //            Logger.LogInfo($"Character ID {absoluteId} name is now: {utils.Utils.GetCharaFieldValue(absoluteId, "name")}");
        //        }
        //    }
        //}

        //public static void PatchSetCharaData(Harmony harmony, string methodName="SetTakashi")
        //{
        //    var dataType = AccessTools.TypeByName("gnosia.Data");
        //    var method = AccessTools.Method(dataType, methodName);
        //    if (method == null)
        //    {
        //        Logger.LogWarning($"Method {methodName} not found in gnosia.Data");
        //        return;
        //    }

        //    var prefix = typeof(SetCharaDataPatch).GetMethod(nameof(SetCharaDataPatch.Prefix), BindingFlags.Static | BindingFlags.Public);
        //    harmony.Patch(method, prefix: new HarmonyMethod(prefix));
        //}

        //public static class SetCharaDataPatch
        //{
        //    public static void Prefix()
        //    {
        //        Logger.LogInfo("A SetX method is about to be called.");
        //    }
        //}


        [HarmonyPatch]
        public class SetTakashiPatch
        {
            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("gnosia.Data");
                return AccessTools.Method(type, "SetKukulsika");
            }

            static void Postfix()
            {
                foreach (var absoluteId in characterIdToFile.Keys)
                {
                    if (characterTexts.TryGetValue(absoluteId, out var character) && !string.IsNullOrEmpty(character.Name))
                    {
                        utils.Utils.SetCharaFieldValue(Logger, absoluteId, "name", character.Name);
                        Logger.LogInfo($"Set name for character ID {absoluteId} to {character.Name}");
                    }
                    // Log the new name
                    Logger.LogInfo($"Character ID {absoluteId} name is now: {utils.Utils.GetCharaFieldValue(absoluteId, "name")}");
                }
            }
        }

        // ScriptParser.SetNormalSerifu
        [HarmonyPatch(typeof(coreSystem.ScriptParser), nameof(coreSystem.ScriptParser.SetNormalSerifu))]
        internal class ScriptParser_SetNormalSerifu_Patch
        {
            internal static void Prefix(coreSystem.ScriptParser __instance, int main,
                int tgt,
                int pos,
                List<string> lang,
                bool waitNextText = false,
                bool withoutTrans = false,
                bool withoutCharaChange = false,
                bool vRole = true)
            {
                Logger.LogInfo($"ScriptParser.SetNormalSerifu called with main={main}, tgt={tgt}, pos={pos}, lang={string.Join(", ", lang)}");
            }
        }
    }
}
