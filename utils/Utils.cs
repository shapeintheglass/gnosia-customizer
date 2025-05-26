using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using gnosia;
using HarmonyLib;
using resource;

namespace GnosiaCustomizer.utils
{
    internal class Utils
    {
        private static readonly Type DataType = AccessTools.TypeByName("gnosia.Data");
        private static readonly Type CharaDataType = AccessTools.Inner(DataType, "CharaData");
        private static readonly FieldInfo CharaField = AccessTools.Field(DataType, "Chara");

        private static GameData? cachedGameData = null;
        private static ResourceManager? cachedResourceManager = null;

        internal static GameData? GetGameDataViaReflection()
        {
            if (cachedGameData != null)
            {
                return cachedGameData;
            }
            // Use reflection to get Data
            FieldInfo gdField = DataType?.GetField("gd", BindingFlags.Public | BindingFlags.Static);
            if (gdField == null)
            {
                return null;
            }
            object gdInstance = gdField.GetValue(null);
            cachedGameData = gdInstance as GameData;
            return cachedGameData;
        }

        internal static ResourceManager? GetResourceManagerViaReflection(application.Screen screen)
        {
            if (cachedResourceManager != null)
            {
                return cachedResourceManager;
            }
            Type screenType = typeof(application.Screen);
            FieldInfo rmField = screenType?.GetField("m_resourceManager", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rmField == null)
            {
                return null;
            }
            object rmInstance = rmField.GetValue(screen);
            cachedResourceManager = rmInstance as ResourceManager;
            return cachedResourceManager;
        }

        internal static object GetCharaFieldValue(int index, string fieldName)
        {
            var charaArray = GetCharaArray();
            if (charaArray == null || index < 0 || index >= charaArray.Length)
                throw new IndexOutOfRangeException($"Invalid CharaData index: {index}");

            var instance = charaArray.GetValue(index);
            var field = AccessTools.Field(CharaDataType, fieldName);
            if (field == null)
            {
                throw new Exception($"Field '{fieldName}' not found in CharaData struct.");
            }
            return field.GetValue(instance);
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
            var charaStruct = array.GetValue(index);

            Logger.LogInfo("Setting character data for index: " + index);
            Logger.LogInfo("Character Name: " + charaText.Name);
            if (!string.IsNullOrEmpty(charaText.Name))
            {
                SetField(charaStruct, "name", charaText.Name);
            }
            Logger.LogInfo("Sex: " + charaText.Sex);
            if (charaText.Sex != null)
            {
                SetField(charaStruct, "sex", charaText.Sex.Value);
            }
            Logger.LogInfo("Age: " + charaText.Age);
            if (charaText.Age != null)
            {
                SetField(charaStruct, "age", charaText.Age.Value);
            }
            Logger.LogInfo("Place: " + charaText.Place);
            if (!string.IsNullOrEmpty(charaText.Place))
            {
                SetField(charaStruct, "d_place", charaText.Place);
            }
            Logger.LogInfo("NumJournalEntries: " + charaText.NumJournalEntries);
            if (charaText.NumJournalEntries != null)
            {
                SetField(charaStruct, "d_tokkiNum", charaText.NumJournalEntries.Value);
            }

            //if (charaText.JournalEntries != null && charaText.JournalEntries.Count > 0)
            //{
            //    var entries = new List<string>(charaText.JournalEntries.Count);
            //    var types = new List<int>(charaText.JournalEntries.Count);
            //    for (int i = 0; i < charaText.JournalEntries.Count; i++)
            //    {
            //        var entry = charaText.JournalEntries[i];
            //        entries[i] = entry.Text;
            //        types[i] = entry.Type;
            //    }
            //    SetField(charaStruct, "d_tokki", entries);
            //    SetField(charaStruct, "d_tokkiType", types);
            //}

            //Logger.LogInfo("Attributes: " + (charaText.Attributes != null ? string.Join(", ", charaText.Attributes) : "null"));
            //if (charaText.Attributes != null && charaText.Attributes.Count > 0)
            //{
            //    var attributes = new List<float>(charaText.Attributes.Count);
            //    foreach (var attr in charaText.Attributes.Values)
            //    {
            //        attributes.Add(attr);
            //    }
            //    SetField(charaStruct, "attr", attributes);
            //}

            //Logger.LogInfo("AbilityStart: " + (charaText.AbilityStart != null ? string.Join(", ", charaText.AbilityStart) : "null"));
            //if (charaText.AbilityStart != null && charaText.AbilityStart.Count > 0)
            //{
            //    var abilityStart = new List<float>(charaText.AbilityStart.Count);
            //    foreach (var ability in charaText.AbilityStart.Values)
            //    {
            //        abilityStart.Add(ability);
            //    }
            //    SetField(charaStruct, "abil", abilityStart);
            //}

            //Logger.LogInfo("AbilityMax: " + (charaText.AbilityMax != null ? string.Join(", ", charaText.AbilityMax) : "null"));
            //if (charaText.AbilityMax != null && charaText.AbilityMax.Count > 0)
            //{
            //    var abilityMax = new List<float>(charaText.AbilityMax.Count);
            //    foreach (var ability in charaText.AbilityMax.Values)
            //    {
            //        abilityMax.Add(ability);
            //    }
            //    SetField(charaStruct, "abilMax", abilityMax);
            //}

            var honorific = GetCharaFieldValue(index, "t_keisho") as string;
            Logger.LogInfo($"Honorific: {honorific}");

            var introduction = GetCharaFieldValue(index, "t_aisatu") as List<string>;
            Logger.LogInfo($"Intro: {introduction[0]}");

            array.SetValue(charaStruct, index);
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


        internal static void PrintCharaFields(int index)
        {
            var charaArray = GetCharaArray();
            var instance = charaArray.GetValue(index);
            var fields = CharaDataType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var value = field.GetValue(instance);
                UnityEngine.Debug.Log($"{field.Name}: {value}");
            }
        }

        private static Array GetCharaArray()
        {
            return (Array)CharaField.GetValue(null); // static field = null instance
        }
    }
}
