using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using static GnosiaCustomizer.patches.TextPatches;

namespace GnosiaCustomizer.utils
{
    internal class CharacterSetter
    {
        private static readonly Type DataType = AccessTools.TypeByName("gnosia.Data");
        private static readonly Type CharaDataType = AccessTools.Inner(DataType, "CharaData");
        private static readonly FieldInfo CharaField = AccessTools.Field(DataType, "Chara");

        private const string Placeholder = "...";

        private const string NameFieldName = "name";
        private const string PlaceFieldName = "d_place";
        private const string HonorificFieldName = "t_keisho";
        private const string JournalFieldName = "d_tokki";
        private const string PersonalFieldName = "t_personal";
        private static readonly Dictionary<string, int> dialogueCount = new Dictionary<string, int>()
        {
            { "t_aisatu", 1 },
            { "t_suspect", 8 },
            { "t_suspect_r", 8 },
            { "t_suspect_add", 1 },
            { "t_suspect_t0", 7 },
            { "t_suspect_t1", 6 },
            { "t_suspected0", 2 },
            { "t_hanron0", 2 },
            { "t_hanron1", 1 },
            { "t_hanron_t0", 1 },
            { "t_hanron_t1", 1 },
            { "t_trust", 3 },
            { "t_trust_r", 3 },
            { "t_trust_t0", 1 },
            { "t_trust_t1", 1 },
            { "t_trusted0", 1 },
            { "t_thanron0", 1 },
            { "t_thanron1", 1 },
            { "t_thanron_t0", 1 },
            { "t_thanron_t1", 1 },
            { "t_hosho", 2 },
            { "t_hosho_enemy", 6 },
            { "t_hosho_miss", 2 },
            { "t_hosho_get", 2 },
            { "t_tohyo_go", 1 },
            { "t_tohyo_mae", 1 },
            { "t_tohyo_sai", 1 },
            { "t_shokei", 2 },
            { "t_wakare", 1 },
            { "t_karare", 1 },
            { "t_imjinro", 1 },
            { "t_shori", 1 },
            { "t_tohyo_kurikaeshi", 1 },
            { "t_tohyo_jeno", 1 },
            { "t_tohyo_alive", 1 },
            { "t_tohyo_jeno_ok", 1 },
            { "t_tohyo_jeno_ng", 1 },
            { "t_tohyo_alive_ok", 1 },
            { "t_tohyo_alive_ng", 1 },
            { "t_tohyo_jeno_kettei", 1 },
            { "t_tohyo_alive_kettei", 1 },
            { "t_tohyo_hitei", 1 },
            { "t_tohyo_dame", 2 },
            { "t_co", 9 },
            { "t_co_find", 9 },
            { "t_co_taiko", 9 },
            { "t_co_req", 1 },
            { "t_co_after", 3 },
            { "t_uranai_o", 1 },
            { "t_uranai_s", 1 },
            { "t_uranai_b", 1 },
            { "t_uranai_t", 1 },
            { "t_uranai_f", 1 },
            { "t_uranai_n", 1 },
            { "t_reibai_s", 2 },
            { "t_reibai_t", 1 },
            { "t_reibai_f", 1 },
            { "t_reibai_ft", 1 },
            { "t_reibai_ff", 1 },
            { "t_houkoku_s", 1 },
            { "t_houkoku_w", 1 },
            { "t_okuyami", 1 },
            { "t_okuyami_n", 1 },
            { "t_okuyami_f", 1 },
            { "t_houkoku_req", 1 },
            { "t_houkoku_not", 1 },
            { "t_skill_sayNingen", 7 },
            { "t_skill_zatsudan", 7 },
            { "t_skill_roller", 7 },
            { "t_skill_doTohyo", 8 },
            { "t_skill_dontTohyo", 4 },
            { "t_skill_kyoryoku", 3 },
            { "t_skill_t_doui", 1 },
            { "t_skill_t_hanronKinshi", 1 },
            { "t_skill_t_kyakushoku", 4 },
            { "t_skill_h_uyamuya", 1 },
            { "t_skill_h_hangeki", 1 },
            { "t_skill_h_dojo", 1 },
            { "t_skill_h_help", 2 },
            { "t_skill_h_careful", 1 },
            { "t_skill_dogeza", 2 },
            { "t_temp", 2 }
        };

        public enum Emotion
        {
            Neutral = 0,
            Happy = 1,
            Annoyed = 2,
            Hurt = 3,
            Surprised = 4,
            Thinking = 5,
            Smug = 6,
            Gnosia = 7
        }

        internal static object GetCharaFieldValue(int index, string fieldName)
        {
            var charaArray = GetCharaArray();
            if (charaArray == null || index < 0 || index >= charaArray.Length)
                throw new IndexOutOfRangeException($"Invalid CharaData index: {index}");

            return GetCharaFieldFromBoxedStruct(fieldName, charaArray.GetValue(index));
        }

        internal static object GetCharaFieldFromBoxedStruct(string fieldName, object charaStructBoxed)
        {
            var field = AccessTools.Field(CharaDataType, fieldName);
            if (field == null)
            {
                throw new Exception($"Field '{fieldName}' not found in CharaData struct.");
            }
            return field.GetValue(charaStructBoxed);
        }

        internal static bool GetCharaFieldValueAsStringArray(int index, string fieldName, out List<string> strArray)
        {
            strArray = null;
            var value = GetCharaFieldValue(index, fieldName);
            if (value is List<string> stringList)
            {
                strArray = stringList;
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool GetCharaFieldAs2dStringArray(int index, string fieldName, out List<List<string>> strArray)
        {
            strArray = null;
            var value = GetCharaFieldValue(index, fieldName);
            if (value is List<List<string>> string2dList)
            {
                strArray = string2dList;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void PreprocessCustomDialogue(List<DialogueLine> dialogue,
            out Dictionary<string, DialogueLine> lines1d,
            out Dictionary<string, List<DialogueLine>> lines2d,
            out Dictionary<string, List<List<DialogueLine>>> lines3d)
        {
            lines1d = new Dictionary<string, DialogueLine>(4); // TODO: Refactor to const
            lines2d = new Dictionary<string, List<DialogueLine>>(dialogueCount.Count);
            lines3d = new Dictionary<string, List<List<DialogueLine>>>(100); // TODO: Refactor to const

            foreach (var line in dialogue)
            {
                if (line.Index.HasValue)
                {
                    if (line.InnerIndex.HasValue)
                    {
                        // 2D dialogue
                        if (!lines3d.TryGetValue(line.Name, out var outerList))
                        {
                            outerList = new List<List<DialogueLine>>();
                            lines3d[line.Name] = outerList;
                        }
                        while (outerList.Count <= line.Index.Value)
                        {
                            outerList.Add(new List<DialogueLine>());
                        }
                        outerList[line.Index.Value].Add(line);
                    }
                    else
                    {
                        // 1D dialogue
                        lines1d[line.Name] = line;
                    }
                }
                else
                {
                    // No index, treat as 1D dialogue
                    lines1d[line.Name] = line;
                }
            }
        }

        internal static void SetChara(ManualLogSource Logger, int index, CharacterText charaText, List<DialogueLine> dialogue)
        {
            Logger.LogInfo($"Setting character data for index {index}");
            var fieldInfo = AccessTools.Field(DataType, "Chara");
            if (fieldInfo == null)
            {
                throw new Exception("Chara field not found in Data class.");
            }
            var array = fieldInfo.GetValue(null) as Array;
            if (array == null)
            {
                throw new Exception("Chara field is not an array or is null.");
            }
            if (index < 0 || index >= array.Length)
            {
                throw new IndexOutOfRangeException("Invalid CharaData index: " + index);
            }
            var charaStructBoxed = array.GetValue(index);

            if (charaText.Sex != null)
            {
                SetField(charaStructBoxed, "sex", charaText.Sex.Value);
            }
            if (charaText.Age != null)
            {
                SetField(charaStructBoxed, "age", charaText.Age.Value);
            }
            if (charaText.NumJournalEntries != null)
            {
                SetField(charaStructBoxed, "d_tokkiNum", charaText.NumJournalEntries.Value);
            }
            if (charaText.HpMin != null)
            {
                SetField(charaStructBoxed, "hpMin", charaText.HpMin.Value);
            }
            if (charaText.HpWithGnos != null)
            {
                SetField(charaStructBoxed, "hpWithGnos", charaText.HpWithGnos.Value);
            }
            if (charaText.Attributes != null)
            {
                var attr = charaText.Attributes;
                if (attr.TryGetValue("playful", out var playful)
                    && attr.TryGetValue("social", out var social)
                    && attr.TryGetValue("logic", out var logic)
                    && attr.TryGetValue("neat", out var neat)
                    && attr.TryGetValue("desire", out var desire)
                    && attr.TryGetValue("courage", out var courage))
                {
                    SetField(charaStructBoxed, "attr", new List<float>()
                    {
                        playful, social, logic, neat, desire, courage
                    });
                }
            }

            if (charaText.AbilityStart != null)
            {
                var abil = charaText.AbilityStart;
                if (abil.TryGetValue("charisma", out var charisma)
                    && abil.TryGetValue("intuition", out var intuition)
                    && abil.TryGetValue("charm", out var charm)
                    && abil.TryGetValue("logic", out var logic)
                    && abil.TryGetValue("perform", out var perform)
                    && abil.TryGetValue("stealth", out var stealth))
                {
                    SetField(charaStructBoxed, "abil", new List<float>()
                    {
                        charisma, intuition, charm, logic, perform, stealth
                    });
                }
            }

            if (charaText.AbilityMax != null)
            {
                var abil = charaText.AbilityMax;
                if (abil.TryGetValue("charisma", out var charisma)
                    && abil.TryGetValue("intuition", out var intuition)
                    && abil.TryGetValue("charm", out var charm)
                    && abil.TryGetValue("logic", out var logic)
                    && abil.TryGetValue("perform", out var perform)
                    && abil.TryGetValue("stealth", out var stealth))
                {
                    SetField(charaStructBoxed, "abilMax", new List<float>()
                    {
                        charisma, intuition, charm, logic, perform, stealth
                    });
                }
            }

            // Pad dialogue fields with placeholders
            foreach (var dialogueName in dialogueCount.Keys)
            {
                Logger.LogInfo($"Processing dialogue field '{dialogueName}' for character index {index}.");
                var field = GetCharaFieldFromBoxedStruct(dialogueName, charaStructBoxed);
                if (field == null)
                {
                    // Create a new list where none previously existed
                    var newList = new List<string>(dialogueCount[dialogueName]);
                    for (int i = 0; i < dialogueCount[dialogueName]; i++)
                    {
                        newList.Add(Placeholder);
                    }
                    SetField(charaStructBoxed, dialogueName, newList);
                }
                else if (field is List<string> stringList)
                {
                    // Modify an existing list
                    if (stringList.Count < dialogueCount[dialogueName])
                    {
                        var newList = new List<string>(dialogueCount[dialogueName]);
                        for (int i = 0; i < dialogueCount[dialogueName]; i++)
                        {
                            newList.Add(Placeholder);
                        }
                        for (int i = 0; i < stringList.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(stringList[i]))
                            {
                                newList[i] = stringList[i];
                            }
                        }

                        SetField(charaStructBoxed, dialogueName, newList);
                    }
                    else
                    {
                        for (int i = 0; i < stringList.Count; i++)
                        {
                            if (string.IsNullOrEmpty(stringList[i]))
                            {
                                stringList[i] = Placeholder;
                            }
                        }
                        SetField(charaStructBoxed, dialogueName, stringList);
                    }
                }
            }
            array.SetValue(charaStructBoxed, index);

            // Go through custom dialogue and replace again (TODO: Optimize second iteration)
            foreach (var dialogueLine in dialogue)
            {
                var dialogueName = dialogueLine.Name;

                if (dialogueCount.TryGetValue(dialogueName, out var count) 
                    && dialogueLine.Index.HasValue
                    && GetCharaFieldValueAsStringArray(dialogueLine.Index.Value, dialogueName, out var strArray))
                {
                    strArray[dialogueLine.Index.Value] = dialogueLine.Text;
                    if (!string.IsNullOrEmpty(dialogueLine.Emotion)
                        && Enum.TryParse<Emotion>(dialogueLine.Emotion, out var emotion))
                    {
                        // Append emotion to the string
                        Logger.LogInfo($"Setting emotion '{emotion}' for dialogue '{dialogueName}' at index {dialogueLine.Index.Value}. String array size: {strArray.Count}.");
                        strArray[dialogueLine.Index.Value] += $"|{(int) emotion}";
                    }
                }
                else if (dialogueName == PersonalFieldName && GetCharaFieldAs2dStringArray(dialogueLine.Index.Value, dialogueName, out var str2dArray))
                {

                }
                else if (dialogueName == NameFieldName || dialogueName == PlaceFieldName || dialogueName == HonorificFieldName)
                {
                    var field = GetCharaFieldFromBoxedStruct(dialogueName, charaStructBoxed);
                    if (field is string name)
                    {
                        // Set the name field
                        SetField(charaStructBoxed, dialogueName, dialogueLine.Text);
                    }
                    else
                    {
                        Logger.LogWarning($"Dialogue field '{dialogueName}' is not a string. Skipping.");
                        continue;
                    }
                }
                else if (dialogueName == JournalFieldName)
                {

                }
                else
                {
                    Logger.LogWarning($"Dialogue field '{dialogueName}' not found in dialogueCount. Skipping.");
                    continue;
                }
            }

            array.SetValue(charaStructBoxed, index);
        }

        private static void SetField(object charaStruct, string fieldName, object value)
        {
            var structType = charaStruct.GetType();
            var targetField = AccessTools.Field(structType, fieldName);
            if (targetField == null)
            {
                throw new Exception($"Field '{fieldName}' not found in CharaData struct.");
            }
            targetField.SetValue(charaStruct, value);
        }


        internal static void LogCharaFieldsToFile(int index, ManualLogSource Logger)
        {
            var charaArray = GetCharaArray();
            var instance = charaArray.GetValue(index);
            var fields = CharaDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var csvFileContents = "Name,Index,InnerIndex,Desc,Emotion,Text\n";
            foreach (var field in fields)
            {
                var value = field.GetValue(instance);
                var lineIndex = 0;

                if (value is List<string> stringList)
                {
                    foreach (var str in stringList)
                    {
                        csvFileContents += ParseEmotionString(str, field.Name, lineIndex++);
                    }
                }
                else if (value is List<List<string>> string2dList)
                {
                    foreach (var innerlist in string2dList)
                    {
                        var innerIndex = 0;
                        foreach (var str in innerlist)
                        {
                            csvFileContents += ParseEmotionString(str, field.Name, lineIndex, innerIndex++);
                        }
                        lineIndex++;
                    }
                } 
                else if (value is string str)
                {
                    csvFileContents += ParseEmotionString(str, field.Name);
                }
            }

            // Write contents of csv to a file
            string filePath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, $"CharaFields_{index}.csv");
            try
            {
                File.WriteAllText(filePath, csvFileContents);
                Logger.LogInfo($"Chara fields logged to {filePath}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to write Chara fields to file: {ex.Message}");
            }
        }

        private static string SanitizeForCsv(string str)
        {
            if (str == null) return "null";
            // Escape double quotes by doubling them
            str = str.Replace("\"", "\"\"");
            // If the string contains a comma, newline, or double quote, wrap it in quotes
            if (str.Contains(",") || str.Contains("\n") || str.Contains("\""))
            {
                str = $"\"{str}\"";
            }
            return str;
        }

        private static string ParseEmotionString(string str, string name, int? index = null, int? innerIndex = null)
        {
            var tokens = str.Split('|');
            var indexOrEmpty = index.HasValue ? index.Value.ToString() : "";
            var innerIndexOrEmpty = innerIndex.HasValue ? innerIndex.Value.ToString() : "";

            if (tokens.Length > 1)
            {
                var emotionInt = int.Parse(tokens[1]) % 100;
                Enum.TryParse<Emotion>(emotionInt.ToString(), out var emotion);
                var text = tokens[0].Trim();
                return $"{name},{indexOrEmpty},{innerIndexOrEmpty},,{emotion},{SanitizeForCsv(text)}\n";
            }
            else
            {
                return $"{name},{indexOrEmpty},{innerIndexOrEmpty},,,{SanitizeForCsv(str)}\n";
            }
        }

        internal static Dictionary<string, int> GetFieldCounts(int index, ManualLogSource Logger)
        {
            var counts = new Dictionary<string, int>();
            var charaArray = GetCharaArray();
            var instance = charaArray.GetValue(index);
            var fields = CharaDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var value = field.GetValue(instance);

                if (value is List<string> stringList)
                {
                    counts[field.Name.ToString()] = stringList.Count;
                }
                else if (value is List<float> floatList)
                {
                    counts[field.Name.ToString()] = floatList.Count;
                }
                else if (value is List<List<string>> string2dList)
                {
                    counts[field.Name.ToString()] = string2dList.Count;
                }
            }
            return counts;
        }

        internal static void PadFields(int index, ManualLogSource Logger)
        {
            var charaArray = GetCharaArray();
            var instance = charaArray.GetValue(index);
            var fields = CharaDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (dialogueCount.TryGetValue(field.Name, out var count))
                {
                    var value = field.GetValue(instance);
                    if (value is List<string> stringList && stringList.Count < count)
                    {
                        // Pad to desired count
                        while (stringList.Count < count)
                        {
                            stringList.Add(Placeholder);
                        }
                        // Write back
                        field.SetValue(instance, stringList);
                    }
                }
            }
        }

        private static Array GetCharaArray()
        {
            return (Array)CharaField.GetValue(null); // static field = null instance
        }
    }
}
