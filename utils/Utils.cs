using System;
using System.Collections;
using System.IO;
using System.Reflection;
using gnosia;
using HarmonyLib;
using resource;
using UnityEngine.Networking;
using UnityEngine;

namespace GnosiaCustomizer.utils
{
    internal class Utils
    {
        private static readonly Type DataType = AccessTools.TypeByName("gnosia.Data");
        private static GameData? cachedGameData = null;
        private static ResourceManager? cachedResourceManager = null;
        private static ScenarioContents[]? cachedScenarioContents = null;

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

        internal static ScenarioContents[] GetScenarioContentsViaReflection()
        {
            if (cachedScenarioContents != null)
            {
                return cachedScenarioContents;
            }

            FieldInfo scField = DataType?.GetField("Scenario", BindingFlags.Public | BindingFlags.Static);
            if (scField == null)
            {
                return Array.Empty<ScenarioContents>();
            }
            object scInstance = scField.GetValue(null);
            if (scInstance is ScenarioContents[] scenarioContents)
            {
                cachedScenarioContents = scenarioContents;
                return scenarioContents;
            }
            else
            {
                Debug.LogError("Failed to retrieve ScenarioContents array via reflection.");
                return Array.Empty<ScenarioContents>();
            }
        }

        public static IEnumerator LoadWavClip(string filePath, Action<AudioClip> onLoaded)
        {
            string uri = "file://" + filePath;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError($"Failed to load audio clip from {filePath}: {www.error}");
                    onLoaded?.Invoke(null);
                }
                else
                {
                    var clip = DownloadHandlerAudioClip.GetContent(www);
                    clip.name = Path.GetFileNameWithoutExtension(filePath);
                    onLoaded?.Invoke(clip);
                }
            }
        }
    }
}
