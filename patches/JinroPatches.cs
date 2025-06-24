using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using gnosia;
using GnosiaCustomizer.utils;
using HarmonyLib;
using setting;

namespace GnosiaCustomizer.patches
{
    public class JinroPatches
    {
        internal static ManualLogSource Logger;

        // Initialized by TextPatches.Initialize()
        public static ConcurrentDictionary<int, HashSet<string>> SkillMap = new();

        // Bit masks for night skills
        private static uint LiarCalloutMask = 0U;
        private static uint VictimRequestMask = 0U;
        private static uint NightCollaborateMask = 0U;

        private const string step_forward = "charisma_step_forward";
        private const string seek_agreement = "charisma_seek_agreement";
        private const string block_argument = "charisma_block_argument";
        private const string say_human = "intuition_say_human";
        private const string dont_be_fooled = "intuition_dont_be_fooled";
        private const string regret = "charm_regret";
        private const string collaborate = "charm_collaborate";
        private const string vote = "logic_vote";
        private const string dont_vote = "logic_dont_vote";
        private const string definite_human_or_enemy = "logic_definite_human_or_enemy";
        private const string definite_ac = "logic_definite_ac";
        private const string definite_bug = "logic_definite_bug";
        private const string freeze_all = "logic_freeze_all";
        private const string retaliate = "perform_retaliate";
        private const string seek_help = "perform_seek_help";
        private const string exaggerate = "perform_exaggerate";
        private const string obfuscate = "stealth_obfuscate";
        private const string small_talk = "stealth_small_talk";
        private const string grovel = "stealth_grovel";
        private const string liar_callout = "night_liar_callout";
        private const string victim_request = "night_victim_request";
        private const string night_collaborate = "night_collaborate";

        internal static void Initialize(ConcurrentDictionary<int, HashSet<string>> skillMap)
        {
            SkillMap = skillMap;

            // Calculate bit masks for night skills
            if (SkillMap != null && SkillMap.Count > 0)
            {
                foreach (var kvp in SkillMap)
                {
                    var cidMask = (uint) 1 << kvp.Key;
                    var skills = kvp.Value;
                    if (skills.Contains(liar_callout))
                    {
                        LiarCalloutMask |= cidMask;
                    }
                    if (skills.Contains(victim_request))
                    {
                        VictimRequestMask |= cidMask;
                    }
                    if (skills.Contains(night_collaborate))
                    {
                        NightCollaborateMask |= cidMask;
                    }
                }
                Logger.LogInfo($"LiarCalloutMask: {LiarCalloutMask}, VictimRequestMask: {VictimRequestMask}, NightCollaborateMask: {NightCollaborateMask}");
            }
        }

        [HarmonyPatch(typeof(Jinro), nameof(Jinro.HaveSkill))]
        internal class JinroHaveSkillPatch
        {
            internal static bool Prefix(ref bool __result, int cid, Setting.SkillList skill)
            {
                if (!SkillMap.ContainsKey(cid) || SkillMap[cid] == null)
                {
                    return true;
                }

                var gameData = Utils.GetGameDataViaReflection();
                if (gameData == null)
                {
                    return true;
                }

                __result = false;
                if (cid < 0 || cid >= (int)gameData.baseData.totalNum) {
                    __result = false;
                    return false;
                }
                if (cid == 0)
                {
                    __result = (gameData.chara[cid].allFlg & (ulong)(1L << (int)(skill & (Setting.SkillList)63))) > 0UL;
                    return false;
                }
                var skillKey = "N/A";
                switch (skill)
                {
                    case Setting.SkillList.sk_Sence_SayNingen:
                        skillKey = say_human;
                        break;
                    case Setting.SkillList.sk_Logic_DoTohyo:
                        skillKey = vote;
                        break;
                    case Setting.SkillList.sk_Logic_DontTohyo:
                        skillKey = dont_vote;
                        break;
                    case Setting.SkillList.sk_Stealth_Zatsudan:
                        skillKey = small_talk;
                        break;
                    case Setting.SkillList.sk_Logic_Roller:
                        skillKey = freeze_all;
                        break;
                    case Setting.SkillList.sk_Perform_Kyoryoku:
                        skillKey = collaborate;
                        break;
                    case Setting.SkillList.sk_Charisma_Doui:
                        skillKey = seek_agreement;
                        break;
                    case Setting.SkillList.sk_Charisma_DontHanron:
                        skillKey = block_argument;
                        break;
                    case Setting.SkillList.sk_Perform_Kyakushoku:
                        skillKey = exaggerate;
                        break;
                    case Setting.SkillList.sk_Stealth_Uyamuya:
                        skillKey = obfuscate;
                        break;
                    case Setting.SkillList.sk_Perform_Hangeki:
                        skillKey = retaliate;
                        break;
                    case Setting.SkillList.sk_Charm_Dojo:
                        skillKey = regret;
                        break;
                    case Setting.SkillList.sk_Perform_Help:
                        skillKey = seek_help;
                        break;
                    case Setting.SkillList.sk_Sence_Careful:
                        skillKey = dont_be_fooled;
                        break;
                    case Setting.SkillList.sk_Stealth_Dogeza:
                        skillKey = grovel;
                        break;
                    case Setting.SkillList.sk_Charisma_Nanoridero:
                        skillKey = step_forward;
                        break;
                    case Setting.SkillList.sk_Logic_SayBlackWhite:
                        skillKey = definite_human_or_enemy;
                        break;
                    case Setting.SkillList.sk_Logic_SayAC:
                        skillKey = definite_ac;
                        break;
                    case Setting.SkillList.sk_Logic_SayBug:
                        skillKey = definite_bug;
                        break;
                    case Setting.SkillList.sk_LAST:
                        __result = false;
                        return false;
                    default:
                        __result = true;
                        return false;
                }
                var absoluteId = gameData.chara[cid].id;
                __result = SkillMap[absoluteId].Contains(skillKey);
                //Logger.LogInfo($"HaveSkill called for cid: {cid}, skill: {skillKey}, result: {__result}");
                return false;
            }

            [HarmonyPatch(typeof(RSUsoScenario), nameof(RSUsoScenario.SetParam))]
            internal class RSUsoScenarioSetParamPatch
            {
                internal static void Postfix(RSUsoScenario __instance)
                {
                    __instance.selMainFlg = LiarCalloutMask;
                }
            }

            [HarmonyPatch(typeof(RSOsouScenario), nameof(RSOsouScenario.SetParam))]
            internal class RSOsouScenarioSetParamPatch
            {
                internal static void Postfix(RSOsouScenario __instance)
                {
                    __instance.selMainFlg = VictimRequestMask;
                }
            }

            [HarmonyPatch(typeof(RSKyoryokuScenario), nameof(RSKyoryokuScenario.SetParam))]
            internal class RSKyoryokuScenarioSetParamPatch
            {
                internal static void Postfix(RSKyoryokuScenario __instance)
                {
                    __instance.selMainFlg = NightCollaborateMask;
                }
            }

            // ResourceManager.GetScenarioBaseText
            //[HarmonyPatch(typeof(ResourceManager), "GetScenarioBaseText")]
            internal static class ResourceManagerGetScenarioBaseTextPatch
            {
                internal static void Postfix(ref string __result, int fileId, int listId, int faceId = -1)
                {
                    Logger.LogInfo($"GetScenarioBaseText called with fileId: {fileId}, listId: {listId}, faceId: {faceId}");
                }
            }

            private static string previousActionName = "";

            //ScenarioEngineObj.MyUpdate
            //[HarmonyPatch(typeof(ScenarioEngineObj), "MyUpdate")]
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
                    if (action.name == previousActionName)
                    {
                        return;
                    }
                    var journalEntries = action.nissi.Join(delimiter: "\n");
                    Logger.LogInfo($"Executing command {action.name}. Priority: {action.priority}.");
                    Logger.LogInfo($"Journal entries: {journalEntries}");
                    Logger.LogInfo($"Action data: id: {actionData.id}, type: {actionData.type}, scenarioNum: {actionData.scenarioNum}, canPlayUser: {actionData.canPlayUser}, targetP: {actionData.canPlayUser}, mainP: {actionData.mainP}, counterP: {actionData.counterP}, tuizuiP: {actionData.tuizuiP}, ctuizuiP: {actionData.ctuizuiP}, power: {actionData.power}, power2: {actionData.power2}, power3: {actionData.power3}, cpower: {actionData.cpower}, cpower2: {actionData.cpower2}, cpower3: {actionData.cpower3}");
                    previousActionName = action.name;
                }
            }
        }
    }
}
