using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace GnosiaCustomizer.patches
{
    internal class SoundPatches : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private static Dictionary<string, AudioClip> cachedAudio = new Dictionary<string, AudioClip>();
        internal static void Initialize()
        {
            Logger.LogInfo("SoundPatches Initialize called");

            // Verify that the sounds folder exists
            string soundFolderPath = Path.Combine(Paths.PluginPath, "sound");
            if (!Directory.Exists(soundFolderPath))
            {
                Logger.LogWarning($"Sound folder not found at {soundFolderPath}. No custom sounds will be loaded.");
                return;
            }

            // Get all available files in the directory
            var availableFiles = new HashSet<string>(
                Directory.GetFiles(soundFolderPath, "*.wav", SearchOption.TopDirectoryOnly)).Select(Path.GetFileNameWithoutExtension);
            foreach (var soundFile in availableFiles)
            {
                Logger.LogInfo($"Found sound file: {soundFile}");
                // Load into an AudioClip
                string filePath = Path.Combine(soundFolderPath, soundFile + ".wav");

                utils.CoroutineRunner.Instance.StartCoroutine(utils.Utils.LoadWavClip(filePath, clip =>
                {
                    if (clip != null)
                    {
                        cachedAudio[soundFile] = clip;
                        Debug.Log($"Loaded custom BGM for {soundFile}");
                    }
                }));

            }

        }
        public static IEnumerator LoadWavFromFile(string path, System.Action<AudioClip> onLoaded)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"Error loading wav file: {www.error}");
                onLoaded?.Invoke(null);
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(www);
            onLoaded?.Invoke(clip);
        }

        [HarmonyLib.HarmonyPatch(typeof(resource.ResourceManager), "GetBGMData")]
        public class ResourceManager_GetBGMData_Patch
        {
            public static void Postfix(resource.ResourceManager __instance, string resourceName, ref AudioClip __result)
            {
                Logger.LogInfo($"ResourceManager.GetBGMData called for {resourceName}");
                if (cachedAudio.TryGetValue(resourceName, out var customAudio))
                {
                    __result = customAudio;
                }
            }
        }

        // ResourceManager.GetVoiceData
        [HarmonyLib.HarmonyPatch(typeof(resource.ResourceManager), "GetVoiceData")]
        public class ResourceManager_GetVoiceData_Patch
        {
            public static void Postfix(resource.ResourceManager __instance, string resourceName, ref AudioClip __result)
            {
                Logger.LogInfo($"ResourceManager.GetVoiceData called for {resourceName}");
                if (cachedAudio.TryGetValue(resourceName, out var customAudio))
                {
                    __result = customAudio;
                }
            }
        }
    }
}
