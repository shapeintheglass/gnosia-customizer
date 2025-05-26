using System;
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

        internal static void SetCharaFieldValue(ManualLogSource Logger, int index, string fieldName, object newValue)
        {
            Logger.LogInfo($"Attempting to set '{fieldName}' at index {index} to '{newValue}'");

            var fieldInfo = AccessTools.Field(DataType, "Chara");
            if (fieldInfo == null)
            {
                Logger.LogInfo("Field 'Chara' not found on DataType");
                return;
            }

            var array = fieldInfo.GetValue(null) as Array;
            if (array == null)
            {
                Logger.LogInfo("Chara array is null");
                return;
            }

            if (index < 0 || index >= array.Length)
            {
                Logger.LogInfo($"Index {index} out of bounds (0..{array.Length - 1})");
                return;
            }

            var charaStruct = array.GetValue(index);
            if (charaStruct == null)
            {
                Logger.LogInfo("charaStruct is null");
                return;
            }

            var structType = charaStruct.GetType();
            var targetField = AccessTools.Field(structType, fieldName);
            if (targetField == null)
            {
                Logger.LogInfo($"Field '{fieldName}' not found in CharaData struct");
                return;
            }

            targetField.SetValue(charaStruct, newValue);
            Logger.LogInfo($"Set '{fieldName}' to '{newValue}' on copy");

            var newValueRead = targetField.GetValue(charaStruct);
            Logger.LogInfo($"'{fieldName}' after set: {newValueRead}");

            array.SetValue(charaStruct, index);
            Logger.LogInfo($"Modified struct written back to array at index {index}");
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
