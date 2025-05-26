using System;
using System.Reflection;
using gnosia;
using HarmonyLib;
using resource;

namespace GnosiaCustomizer.utils
{
    internal class Utils
    {
        private static GameData? cachedGameData = null;
        private static ResourceManager? cachedResourceManager = null;

        internal static GameData? GetGameDataViaReflection()
        {
            if (cachedGameData != null)
            {
                return cachedGameData;
            }
            // Use reflection to get Data
            Type dataType = AccessTools.TypeByName("gnosia.Data");
            FieldInfo gdField = dataType?.GetField("gd", BindingFlags.Public | BindingFlags.Static);
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
    }
}
