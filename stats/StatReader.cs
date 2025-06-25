using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx;
using BepInEx.Logging;
using coreSystem;
using gnosia;
using GnosiaCustomizer.utils;
using HarmonyLib;

namespace GnosiaCustomizer.stats
{
    internal class StatReader
    {
        internal static new ManualLogSource Logger;
        private const string DataFileName = "data.js";
        private const string DataFileTemplate = "window.rawData = {0};";

        private static string PreviousActionName = "";

        private static Dictionary<int, CharaStats> CharaStatDict = new Dictionary<int, CharaStats>();

        public class CharaStats
        {
            public string Name { get; set; }
            public uint GameId { get; set; }
            public uint AbsoluteId { get; set; }
            public string Role { get; set; }
            public string PersonaRole { get; set; }
            public string Status { get; set; }
            public float Hate { get; set; }
            public float HP { get; set; }
            public float Score { get; set; }
            public float TScore { get; set; }
            public float AvgTrust { get; set; }
            public float AbilCharisma { get; set; }
            public float AbilIntuition { get; set; }
            public float AbilCharm { get; set; }
            public float AbilLogic { get; set; }
            public float AbilPerform { get; set; }
            public float AbilStealth { get; set; }
            public float AttrPlayful { get; set; }
            public float AttrSocial { get; set; }
            public float AttrLogic { get; set; }
            public float AttrNeat { get; set; }
            public float AttrDesire { get; set; }
            public float AttrCourage { get; set; }
            public List<float> PersonaTrust { get; set; }
            public List<float> InternalTrust { get; set; }
            public List<float> Friendship { get; set; }
            public List<float> Love { get; set; }
            public List<string> RoleKnowledge { get; set; }
        }

        private static void GetStats(GameData gd)
        {
            for (var cid = 0; cid < gd.chara.Count; cid++)
            {
                var charaData = gd.chara[cid];
                var absoluteId = charaData.id;
                Logger.LogInfo($"Processing character index {cid} with absolute ID {absoluteId}.");
                CharacterSetter.GetCharaFieldValueAsString(absoluteId, "name", out var name);

                CharaStatDict[absoluteId] = new CharaStats()
                {
                    Name = name,
                    GameId = (uint) cid,
                    AbsoluteId = absoluteId,
                    Role = ConvertRole(charaData.i_yaku),
                    PersonaRole = ConvertRole(charaData.p_yaku),
                    Status = ConvertStatus(charaData.doa),
                    Hate = charaData.hate,
                    HP = gd.GetHp(cid),
                    Score = charaData.gnos,
                    TScore = charaData.tgnos,
                    AbilCharisma = gd.GetAbil(cid, setting.Setting.E_abil.ab_Charisma),
                    AbilIntuition = gd.GetAbil(cid, setting.Setting.E_abil.ab_Sence),
                    AbilCharm = gd.GetAbil(cid, setting.Setting.E_abil.ab_Charm),
                    AbilLogic = gd.GetAbil(cid, setting.Setting.E_abil.ab_Logic),
                    AbilPerform = gd.GetAbil(cid, setting.Setting.E_abil.ab_Perform),
                    AbilStealth = gd.GetAbil(cid, setting.Setting.E_abil.ab_Stealth),
                    AttrPlayful = gd.GetAttr(cid, setting.Setting.E_att.at_Playful),
                    AttrSocial = gd.GetAttr(cid, setting.Setting.E_att.at_Social),
                    AttrLogic = gd.GetAttr(cid, setting.Setting.E_att.at_Logic),
                    AttrNeat = gd.GetAttr(cid, setting.Setting.E_att.at_Neat),
                    AttrDesire = gd.GetAttr(cid, setting.Setting.E_att.at_Desire),
                    AttrCourage = gd.GetAttr(cid, setting.Setting.E_att.at_Courage),
                    PersonaTrust = charaData.p_trust,
                    InternalTrust = charaData.i_trust,
                    Friendship = charaData.friendship,
                    Love = charaData.love,
                    RoleKnowledge = charaData.knowledge.Select(ConvertRole).ToList(),
                };
            }
        }

        private static string ConvertStatus(setting.Setting.Doa deadOrAlive)
        {
            return deadOrAlive switch
            {
                setting.Setting.Doa.doa_Seizon => "Alive",
                setting.Setting.Doa.doa_Kamare => "Killed",
                setting.Setting.Doa.doa_Shokei => "Cold Sleep",
                setting.Setting.Doa.doa_Fumei => "Missing",
                _ => "?",
            };
        }

        private static string ConvertRole(setting.Setting.Yakuwari yakuwari)
        {
            return yakuwari switch
            {
                setting.Setting.Yakuwari.y_Uranai => "Engineer",
                setting.Setting.Yakuwari.y_Reibai => "Doctor",
                setting.Setting.Yakuwari.y_Kari => "Guardian Angel",
                setting.Setting.Yakuwari.y_Lover => "Guard Duty",
                setting.Setting.Yakuwari.y_Murabito => "Crew",
                setting.Setting.Yakuwari.y_Kyojin => "AC Follower",
                setting.Setting.Yakuwari.y_Jinro => "Gnosia",
                setting.Setting.Yakuwari.y_Fox => "Bug",
                _ => "?",
            };
        }

        [HarmonyPatch(typeof(ScenarioEngineObj), nameof(ScenarioEngineObj.MyUpdate))]
        internal static class ScenarioEngineObjMyUpdatePatch
        {
            internal static void Postfix(ScenarioEngineObj __instance)
            {
                var gd = Utils.GetGameDataViaReflection();
                if (gd == null)
                {
                    return;
                }

                var scenarioContents = Utils.GetScenarioContentsViaReflection();
                if (scenarioContents == null)
                {
                    return;
                }
                var actionData = gd.actionDoIt;
                if (actionData.scenarioNum < 0 || actionData.id < 0)
                {
                    return;
                }
                if (actionData.scenarioNum >= gd.sceOn.Count || gd.sceOn[actionData.scenarioNum].id >= scenarioContents.Length
                    || actionData.id >= scenarioContents[gd.sceOn[actionData.scenarioNum].id].actions.Count)
                {
                    return;
                }
                var action = scenarioContents[gd.sceOn[actionData.scenarioNum].id].actions[actionData.id];
                if (action.name == PreviousActionName)
                {
                    return;
                }

                GetStats(gd);

                try
                {
                    var filePath = Path.Combine(Paths.PluginPath, "stats", DataFileName);
                    var jsonData = JsonSerializer.Serialize(CharaStatDict);
                    var toWrite = string.Format(DataFileTemplate, jsonData);
                    File.WriteAllText(filePath, toWrite);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to write stats to file: {ex.Message}");
                }

                Logger.LogInfo($"Executing command {action.name}. Priority: {action.priority}.");
                Logger.LogInfo($"Action data: id: {actionData.id}, type: {actionData.type}, scenarioNum: {actionData.scenarioNum}, canPlayUser: {actionData.canPlayUser}, targetP: {actionData.canPlayUser}, mainP: {actionData.mainP}, counterP: {actionData.counterP}, tuizuiP: {actionData.tuizuiP}, ctuizuiP: {actionData.ctuizuiP}, power: {actionData.power}, power2: {actionData.power2}, power3: {actionData.power3}, cpower: {actionData.cpower}, cpower2: {actionData.cpower2}, cpower3: {actionData.cpower3}");
                PreviousActionName = action.name;
            }
        }
    }
}
