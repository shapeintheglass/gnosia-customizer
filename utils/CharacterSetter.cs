using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace GnosiaCustomizer.utils
{
    internal class CharacterSetter
    {
        private static readonly Type DataType = AccessTools.TypeByName("gnosia.Data");
        private static readonly Type CharaDataType = AccessTools.Inner(DataType, "CharaData");
        private static readonly FieldInfo CharaField = AccessTools.Field(DataType, "Chara");

        private const string Placeholder = "...";

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
        };

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

        internal static List<string> GetCharaFieldValueAsStringArray(int index, string fieldName)
        {
            var value = GetCharaFieldValue(index, fieldName);
            if (value is List<string> stringList)
            {
                return stringList;
            }
            else if (value is string singleString)
            {
                return new List<string> { singleString };
            }
            else
            {
                throw new Exception($"Field '{fieldName}' is not a List<string> or string.");
            }
        }

        internal static void SetChara(ManualLogSource Logger, int index, CharacterText charaText)
        {
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

            if (!string.IsNullOrEmpty(charaText.Name))
            {
                SetField(charaStructBoxed, "name", charaText.Name);
            }
            if (charaText.Sex != null)
            {
                SetField(charaStructBoxed, "sex", charaText.Sex.Value);
            }
            if (charaText.Age != null)
            {
                SetField(charaStructBoxed, "age", charaText.Age.Value);
            }
            if (!string.IsNullOrEmpty(charaText.Place))
            {
                SetField(charaStructBoxed, "d_place", charaText.Place);
            }
            if (charaText.NumJournalEntries != null)
            {
                SetField(charaStructBoxed, "d_tokkiNum", charaText.NumJournalEntries.Value);
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

            foreach (var skillName in dialogueCount.Keys)
            {
                var field = GetCharaFieldFromBoxedStruct(skillName, charaStructBoxed);
                if (field == null)
                {
                    var newList = new List<string>(dialogueCount[skillName]);
                    for (int i = 0; i < dialogueCount[skillName]; i++)
                    {
                        newList.Add(Placeholder);
                    }
                    SetField(charaStructBoxed, skillName, newList);
                }
                else if (field is List<string> stringList)
                {
                    if (stringList.Count < dialogueCount[skillName])
                    {
                        var newList = new List<string>(dialogueCount[skillName]);
                        for (int i = 0; i < dialogueCount[skillName]; i++)
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

                        SetField(charaStructBoxed, skillName, newList);
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
                        SetField(charaStructBoxed, skillName, stringList);
                    }
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


        internal static void LogCharaFields(int index, ManualLogSource Logger)
        {
            var charaArray = GetCharaArray();
            var instance = charaArray.GetValue(index);
            var fields = CharaDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var value = field.GetValue(instance);

                if (value is List<string> stringList)
                {
                    value = string.Join(", ", stringList);
                }
                else if (value is List<float> floatList)
                {
                    value = string.Join(", ", floatList);
                }
                else if (value is Array array)
                {
                    value = string.Join(", ", array);
                }
                else if (value is List<List<string>> string2dList)
                {
                    // Print 2d list
                    value = string.Join("; ", string2dList.ConvertAll(innerList => string.Join(", ", innerList)));
                }
                else if (value is null)
                {
                    value = "null";
                }

                 Logger.LogInfo($"{field.Name}: {value}");
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
