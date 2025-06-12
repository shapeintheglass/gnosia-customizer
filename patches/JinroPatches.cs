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
        internal static new ManualLogSource Logger;

        // Initialized by TextPatches.Initialize()
        public static Dictionary<int, Dictionary<string, bool>> SkillMap = new Dictionary<int, Dictionary<string, bool>>();

        private const string step_forward = "charisma_step_forward";
        private const string seek_agreement = "charisma_seek_agreement";
        private const string block_argument = "charisma_block_argument";
        private const string say_human = "intuition_say_human";
        private const string dont_be_fooled = "intuition_dont_be_fooled";
        private const string regret = "charm_regret";
        private const string collaborate = "charm_collaborate";
        private const string vote = "logic_vote";
        private const string dont_vote = "logic_dont_vote";
        private const string definite_human = "logic_definite_human";
        private const string definite_enemy = "logic_definite_enemy";
        private const string freeze_all = "logic_freeze_all";
        private const string retaliate = "perform_retaliate";
        private const string seek_help = "perform_seek_help";
        private const string exaggerate = "perform_exaggerate";
        private const string obfuscate = "stealth_obfuscate";
        private const string small_talk = "stealth_small_talk";
        private const string grovel = "stealth_grovel";

        [HarmonyPatch(typeof(Jinro), "HaveSkill")]
        internal class JinroHaveSkillPatch
        {
            internal static bool Prefix(ref bool __result, int cid, Setting.SkillList skill)
            {
                if (cid == 0)
                {
                    return true;
                }
                if (!SkillMap.ContainsKey(cid) || SkillMap[cid] == null)
                {
                    //Logger.LogWarning($"Character {cid} not found in SkillMap. Falling back to original skills.");
                    return true;
                }

                __result = false;
                var gameData = Utils.GetGameDataViaReflection();
                if (gameData == null)
                {
                    Logger.LogError("GameData is null, cannot check skills.");
                    return false;
                }

                if (cid < 0 || cid >= (int)gameData.baseData.totalNum) {
                    __result = false;
                    return false;
                }
                if (cid == 0)
                {
                    __result = (gameData.chara[cid].allFlg & (ulong)(1L << (int)(skill & (Setting.SkillList)63))) > 0UL;
                    return false;
                }
                switch (skill)
                {
                    case Setting.SkillList.sk_Sence_SayNingen:
                        __result = SkillMap[cid].TryGetValue(say_human, out var hasSayHuman) && hasSayHuman;
                        break;
                    case Setting.SkillList.sk_Logic_DoTohyo:
                        __result = SkillMap[cid].TryGetValue(vote, out var hasVote) && hasVote;
                        break;
                    case Setting.SkillList.sk_Logic_DontTohyo:
                        __result = SkillMap[cid].TryGetValue(dont_vote, out var hasDontVote) && hasDontVote;
                        break;
                    case Setting.SkillList.sk_Stealth_Zatsudan:
                        __result = SkillMap[cid].TryGetValue(small_talk, out var hasSmallTalk) && hasSmallTalk;
                        break;
                    case Setting.SkillList.sk_Logic_Roller:
                        __result = SkillMap[cid].TryGetValue(freeze_all, out var hasFreezeAll) && hasFreezeAll;
                        break;
                    case Setting.SkillList.sk_Perform_Kyoryoku:
                        __result = SkillMap[cid].TryGetValue(collaborate, out var hasCollaborate) && hasCollaborate;
                        break;
                    case Setting.SkillList.sk_Charisma_Doui:
                        __result = SkillMap[cid].TryGetValue(seek_agreement, out var hasSeekAgreement) && hasSeekAgreement;
                        break;
                    case Setting.SkillList.sk_Charisma_DontHanron:
                        __result = SkillMap[cid].TryGetValue(block_argument, out var hasBlockArgument) && hasBlockArgument;
                        break;
                    case Setting.SkillList.sk_Perform_Kyakushoku:
                        __result = SkillMap[cid].TryGetValue(exaggerate, out var hasExaggerate) && hasExaggerate;
                        break;
                    case Setting.SkillList.sk_Stealth_Uyamuya:
                        __result = SkillMap[cid].TryGetValue(obfuscate, out var hasObfuscate) && hasObfuscate;
                        break;
                    case Setting.SkillList.sk_Perform_Hangeki:
                        __result = SkillMap[cid].TryGetValue(retaliate, out var hasRetaliate) && hasRetaliate;
                        break;
                    case Setting.SkillList.sk_Charm_Dojo:
                        __result = SkillMap[cid].TryGetValue(regret, out var hasRegret) && hasRegret;
                        break;
                    case Setting.SkillList.sk_Perform_Help:
                        __result = SkillMap[cid].TryGetValue(seek_help, out var hasSeekHelp) && hasSeekHelp;
                        break;
                    case Setting.SkillList.sk_Sence_Careful:
                        __result = SkillMap[cid].TryGetValue(dont_be_fooled, out var hasDontBeFooled) && hasDontBeFooled;
                        break;
                    case Setting.SkillList.sk_Stealth_Dogeza:
                        __result = SkillMap[cid].TryGetValue(grovel, out var hasGrovel) && hasGrovel;
                        break;
                    default:
                        __result = true;
                        break;
                }
                return false; // Skip original method
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
