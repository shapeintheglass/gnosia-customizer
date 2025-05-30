using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using GnosiaCustomizer.utils;
using resource;
using UnityEngine;
using UnityEngine.Networking;

namespace GnosiaCustomizer.patches
{
    internal class SoundPatches
    {
        internal static new ManualLogSource Logger;

        private static Dictionary<string, AudioClip> cachedAudio = new Dictionary<string, AudioClip>();
        internal static void Initialize()
        {
            // Verify that the sounds folder exists
            string soundFolderPath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, Consts.AudioAssetsFolder);
            if (!Directory.Exists(soundFolderPath))
            {
                Logger.LogWarning($"Audio folder not found at {soundFolderPath}. No custom sounds will be loaded.");
                return;
            }

            // Get all available files in the directory
            var availableFiles = new HashSet<string>(
                Directory.GetFiles(soundFolderPath, "*.wav", SearchOption.TopDirectoryOnly)).Select(Path.GetFileNameWithoutExtension);
            Logger.LogInfo($"Found {availableFiles.Count()} sound files in {soundFolderPath}.");
            foreach (var soundFile in availableFiles)
            {
                string filePath = Path.Combine(soundFolderPath, soundFile + ".wav");

                CoroutineRunner.Instance.StartCoroutine(Utils.LoadWavClip(filePath, clip =>
                {
                    if (clip != null)
                    {
                        cachedAudio[soundFile] = clip;
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
                onLoaded?.Invoke(null);
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(www);
            onLoaded?.Invoke(clip);
        }

        [HarmonyLib.HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetBGMData))]
        public class ResourceManager_GetBGMData_Patch
        {
            public static void Postfix(ResourceManager __instance, string resourceName, ref AudioClip __result)
            {
                Logger.LogInfo($"ResourceManager.GetBGMData called for {resourceName}");
                if (cachedAudio.TryGetValue(resourceName, out var customAudio))
                {
                    __result = customAudio;
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(ResourceManager), nameof(ResourceManager.GetVoiceData))]
        public class ResourceManager_GetVoiceData_Patch
        {
            public static void Postfix(ResourceManager __instance, string resourceName, ref AudioClip __result)
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
