using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;

namespace GnosiaCustomizer.utils
{
    internal class GenerateOriginalConfig
    {
        // All skills that characters can have
        private static List<string> AllSkills =
        [
            "charisma_step_forward",
            "charisma_seek_agreement",
            "charisma_block_argument",
            "intuition_say_human",
            "intuition_dont_be_fooled",
            "charm_regret",
            "charm_collaborate",
            "logic_vote",
            "logic_dont_vote",
            "logic_definite_human_or_enemy",
            "logic_definite_ac",
            "logic_definite_bug",
            "logic_freeze_all",
            "perform_retaliate",
            "perform_seek_help",
            "perform_exaggerate",
            "stealth_obfuscate",
            "stealth_small_talk",
            "stealth_grovel",
            "night_liar_callout",
            "night_collaborate",
            "night_victim_request",
        ];

        // Keeping track of which skills are originally held by each character
        internal static Dictionary<uint, HashSet<string>> OriginalSkills = new()
        {
            {1 /* Gina       */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "intuition_say_human", "intuition_dont_be_fooled", "logic_dont_vote", "night_liar_callout", "night_victim_request" } },
            {2 /* SQ         */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "stealth_small_talk", "charm_collaborate", "perform_exaggerate", "stealth_obfuscate", "charm_regret", "perform_seek_help", "night_liar_callout", "night_victim_request" } },
            {3 /* Raqio      */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "logic_vote", "logic_dont_vote", "logic_freeze_all", "perform_exaggerate", "perform_retaliate", "night_collaborate", "night_victim_request" } },
            {4 /* Stella     */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "logic_vote", "logic_dont_vote", "logic_freeze_all", "perform_seek_help", "night_collaborate", "night_victim_request" } },
            {5 /* Shigemichi */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "stealth_small_talk", "charisma_seek_agreement", "charisma_block_argument", "stealth_obfuscate", "night_collaborate", "night_victim_request" } },
            {6 /* Chipie     */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "stealth_small_talk", "charm_collaborate", "intuition_dont_be_fooled", "night_liar_callout", "night_victim_request" } },
            {7 /* Remnan     */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "charm_collaborate", "perform_retaliate", "charm_regret", "perform_seek_help", "intuition_dont_be_fooled", "night_liar_callout", "night_collaborate", "night_victim_request" } },
            {8 /* Comet      */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "intuition_say_human", "intuition_dont_be_fooled", "night_liar_callout", "night_collaborate", "night_victim_request" } },
            {9 /* Yuriko     */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "intuition_say_human", "logic_vote", "logic_dont_vote", "logic_freeze_all", "charm_collaborate", "charisma_seek_agreement", "charisma_block_argument", "perform_retaliate", "perform_seek_help" } },
            {10 /* Jonas     */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "intuition_say_human", "logic_vote", "stealth_small_talk", "charisma_seek_agreement", "perform_exaggerate", "stealth_obfuscate", "night_liar_callout", "night_collaborate", "night_victim_request" } },
            {11 /* Setsu     */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "intuition_say_human", "logic_vote", "logic_dont_vote", "logic_freeze_all", "charisma_seek_agreement", "perform_retaliate", "night_victim_request" } },
            {12 /* Otome     */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "logic_dont_vote", "logic_freeze_all", "charm_regret", "night_liar_callout", "night_collaborate", "night_victim_request" } },
            {13 /* Sha-ming  */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "stealth_small_talk", "charm_collaborate", "charisma_seek_agreement", "perform_exaggerate", "stealth_obfuscate", "perform_retaliate", "perform_seek_help", "stealth_grovel", "night_liar_callout", "night_victim_request" } },
            {14 /* Kukrushka */, new HashSet<string> { "charisma_step_forward", "logic_definite_human_or_enemy", "logic_definite_ac", "logic_definite_bug", "charm_collaborate", "perform_exaggerate", "charm_regret", "perform_seek_help", "night_liar_callout", "night_collaborate", "night_victim_request" } }
        };

        internal static void WriteCharaDataToFile(int absoluteId)
        {
            Console.WriteLine($"Generating config for character ID {absoluteId}...");
            var fieldInfo = AccessTools.Field(CharacterSetter.DataType, "Chara");
            if (fieldInfo == null)
            {
                throw new Exception("Chara field not found in Data class.");
            }
            var array = fieldInfo.GetValue(null) as Array;
            if (array == null)
            {
                throw new Exception("Chara field is not an array or is null.");
            }
            if (absoluteId < 0 || absoluteId >= array.Length)
            {
                throw new IndexOutOfRangeException("Invalid CharaData index: " + absoluteId);
            }
            var charaStructBoxed = array.GetValue(absoluteId);
            var contents = "";

            if (CharacterSetter.GetCharaFieldValueAsString(absoluteId, "name", out var name))
            {
                contents += "# How the game and other people will address this character.\n";
                contents += $"name: {name}\n";
            }
            if (CharacterSetter.GetCharaFieldValue(absoluteId, "sex", out var sexObj) && sexObj is byte sexByte)
            {
                contents += "# 0 = Male, 1 = Female, 2 = Non-binary.\n";
                contents += $"sex: {sexByte}\n";
            }
            if (CharacterSetter.GetCharaFieldValueAsString(absoluteId, "d_place", out var origin))
            {
                contents += "# Where the character is from. Only shown on their bio page.\n";
                contents += $"origin: {Sanitize(origin)}\n";
            }
            if (CharacterSetter.GetCharaFieldValue(absoluteId, "age", out var ageObj) && ageObj is uint ageUint)
            {
                contents += "# The character's age. Only shown on their bio page.\n";
                contents += $"age: {ageUint}\n";
            }
            if (CharacterSetter.GetCharaFieldValueAsStringArray(absoluteId, "t_temp", out var tempLines))
            {
                contents += "# Information about this character. Only shown on their bio page.\n";
                contents += $"bio1: {Sanitize(tempLines[0])}\n";
                if (tempLines.Count > 1)
                {
                    contents += $"bio2: {Sanitize(tempLines[1])}\n";
                }
                else
                {
                    contents += "bio2: \"\"\n"; // Ensure bio2 is present even if empty
                }
            }
            if (CharacterSetter.GetCharaFieldValueAsString(absoluteId, "t_keisho", out var honorific))
            {
                contents += "# Honorific when they address other people (ex. -san, -sama, etc). Leave blank for no honorific.\n";
                contents += $"honorific: {Sanitize(honorific)}\n\n";
            }
            if (CharacterSetter.GetCharaFieldValue(absoluteId, "hpMin", out var defenseMinObj) && defenseMinObj is float defenseMin)
            {
                contents += "# Their base 'defense' rating against doubt.\n";
                contents += $"defense_min: {defenseMin}\n";
            }
            if (CharacterSetter.GetCharaFieldValue(absoluteId, "hpWithGnos", out var defenseWithGnosObj) && defenseWithGnosObj is float defenseWithGnos)
            {
                contents += "# Their base 'defense' rating against doubt, as a Gnosia.\n";
                contents += $"defense_with_gnos: {defenseWithGnos}\n\n";
            }
            if (CharacterSetter.GetCharaFieldValue(absoluteId, "attr", out var attrObj) && attrObj is List<float> attrList)
            {
                contents += "# Affects the character's likelihood to perform certain actions.\n";
                contents += "attributes:\n";
                contents += $"  playful: {attrList[0]}\n";
                contents += $"  social: {attrList[1]}\n";
                contents += $"  logic: {attrList[2]}\n";
                contents += $"  neat: {attrList[3]}\n";
                contents += $"  desire: {attrList[4]}\n";
                contents += $"  courage: {attrList[5]}\n\n";
            }
            if (CharacterSetter.GetCharaFieldValue(absoluteId, "abil", out var abilObj) && abilObj is List<float> abilList)
            {
                contents += "# The character's starting ability for each type.\n";
                contents += "abilityStart:\n";
                contents += $"  charisma: {abilList[0]}\n";
                contents += $"  intuition: {abilList[1]}\n";
                contents += $"  charm: {abilList[2]}\n";
                contents += $"  logic: {abilList[3]}\n";
                contents += $"  perform: {abilList[4]}\n";
                contents += $"  stealth: {abilList[5]}\n\n";
            }
            if (CharacterSetter.GetCharaFieldValue(absoluteId, "abilMax", out var abilMaxObj) && abilMaxObj is List<float> abilMaxList)
            {
                contents += "# The character's maximum ability for each type.\n";
                contents += "abilityMax:\n";
                contents += $"  charisma: {abilMaxList[0]}\n";
                contents += $"  intuition: {abilMaxList[1]}\n";
                contents += $"  charm: {abilMaxList[2]}\n";
                contents += $"  logic: {abilMaxList[3]}\n";
                contents += $"  perform: {abilMaxList[4]}\n";
                contents += $"  stealth: {abilMaxList[5]}\n\n";
            }

            // Skills
            if (absoluteId > 0)
            {
                contents += "# Skills the character is capable of using. If a skill is not present here, false is assumed.\n";
                contents += "known_skills:\n";
                var skillSet = OriginalSkills[(uint)absoluteId];
                foreach (var skillName in AllSkills)
                {
                    contents += $"  {skillName}: {skillSet.Contains(skillName).ToString().ToLower()}\n";
                }
                contents += "\n\n############\r\n# DIALOGUE #\r\n############\n\n";
            }

            //
            // Dialogue
            //
            var d = new Dictionary<string, List<string>>();
            foreach (var key in CharacterSetter.DialogueInitialization.Keys)
            {
                if (CharacterSetter.GetCharaFieldValueAsStringArray(absoluteId, key, out var strArray))
                {
                    d[key] = strArray;
                }
                else
                {
                    d[key] = ["..."];
                }
                // Just in case, pad additional blanks at the end
                while (d[key].Count < 8)
                {
                    d[key].Add("...");
                }
            }
            if (!CharacterSetter.GetCharaFieldAs2dStringArray(absoluteId, "t_personal", out var personalArray))
            {
                // Initialize blank 20x20 list if not found
                personalArray = new List<List<string>>();
                for (int i = 0; i < 20; i++)
                {
                    personalArray.Add(new List<string>() { "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "...", "..." });
                }
            }
            contents += WriteDialogue("opening_statement", "Statement at the start of the round.", personalArray[0][16]);
            contents += WriteDialogue("opening_remarks_condolences", "Mourning the loss of {0} during the night phase.", d["t_okuyami"][0]);
            contents += WriteDialogue("opening_remarks_no_deaths", "Celebrating that no one disappeared last night.", d["t_okuyami_n"][0]);
            contents += WriteDialogue("opening_remarks_two_deaths", "Reacting to the deaths of {0} ('Name1 and Name2') during the night phase. One of them was the bug, eliminated by the Engineer.", d["t_okuyami_f"][0]);
            contents += WriteDialogue("step_forward", "Requesting that whoever has the role of {0} step forward.", d["t_co_req"][0]);
            contents += WriteDialogue("reveal_role_engineer", "First person to reveal role as {0} ('Engineer'). They can investigate one person every night.", d["t_co"][1]);
            contents += WriteDialogue("reveal_own_role_engineer", "Counter-claiming {1} (single or multiple names) for the role of {0} ('Engineer').", d["t_co_taiko"][1]);
            contents += WriteDialogue("reveal_role_engineer_with_gnosia_result", "Revealing role as {0} ('Engineer') while also stating {1} is Gnosia.", d["t_co_find"][1]);
            contents += WriteDialogue("reveal_role_doctor", "First to reveal role as {0} ('Doctor'). They can investigate whoever was put into cold sleep the previous night.", d["t_co"][2]);
            contents += WriteDialogue("reveal_own_role_doctor", "Counter-claiming {1} (single or multiple names) for the role of {0} ('Doctor').", d["t_co_taiko"][2]);
            contents += WriteDialogue("reveal_role_doctor_with_gnosia_result", "Revealing role as {0} ('Doctor') while also stating the person {1} was Gnosia.", d["t_co_find"][2]);
            contents += WriteDialogue("reveal_role_guard_duty", "First to reveal role as {0} ('Guard Duty'). The other person on guard duty will corroborate this.", d["t_co"][4]);
            contents += WriteDialogue("reveal_role_guard_duty_response", "Corroborating {1}'s claim of being {0} ('Guard Duty').", d["t_co_taiko"][4]);
            contents += WriteDialogue("reveal_role_guardian_angel", "First to reveal role as {0} ('Guardian Angel').", d["t_co"][3]);
            contents += WriteDialogue("step_forward_1_claim_reaction", "Reacting to {1} being the sole claimant to the role of {0} (i.e. they're the genuine {0}).", d["t_co_after"][0]);
            contents += WriteDialogue("step_forward_2_claims_reaction", "Reacting to {1} ('Name1 and Name2') being two claimants to the role of {0}.", d["t_co_after"][1]);
            contents += WriteDialogue("step_forward_3_claims_reaction", "Reacting to {1} ('Name1, Name2, and Name3') being three or more claimants to the role of {0}.", d["t_co_after"][2]);
            contents += WriteDialogue("request_report", "Asking for any Engineer/Doctor who hasn't spoken yet to present their report.", d["t_houkoku_req"][0]);
            contents += WriteDialogue("request_report_no_response", "One of the Doctors/Engineers, named {0}, failed to present their report. This is very incriminating.", d["t_houkoku_not"][0]);
            contents += WriteDialogue("report_pt1_matching_result", "The first part of the report statement as a Doctor or Engineer who is following up after someone else's report. They examined the same person last night, {1}, and got the same result. {0} = the string 'Gnosia'.", d["t_houkoku_s"][0]);
            contents += WriteDialogue("report_pt1_conflicting_result", "The first part of the report statement as a Doctor or Engineer who is following up after someone else's report. They examined the same person last night, {0}, but got a different result. {0} = the string 'Gnosia'.", d["t_houkoku_w"][0]);
            contents += WriteDialogue("report_engineer_pt1_target_killed", "As {0} ('Engineer') giving their report first, the first sentence of their report that they investigated {1}, who was killed last night.", d["t_uranai_o"][0]);
            contents += WriteDialogue("report_engineer_pt1", "As {0} ('Engineer') giving their report first, the first sentence of their report that they investigated {1}.", d["t_uranai_s"][0]);
            contents += WriteDialogue("report_engineer_pt1_different_target", " As {0} ('Engineer'), the first sentence of their report that they investigated a different target than what the other Engineer reported.", d["t_uranai_b"][0]);
            contents += WriteDialogue("report_engineer_pt2_gnosia_result", "As Engineer, the second sentence of their report that {1} was {0} ('Gnosia').", d["t_uranai_t"][0]);
            contents += WriteDialogue("report_engineer_pt2_human_result", "As Engineer, the second sentence of their report that {1} was NOT {0} ('Gnosia').", d["t_uranai_f"][0]);
            contents += WriteDialogue("report_engineer_no_result", "As Engineer, stating that they did not investigate anyone as there were no more valid targets to examine.", d["t_uranai_n"][0]);
            contents += WriteDialogue("report_doctor_pt1", "# As Doctor, the first sentence of their report that they investigated the people who were in cold sleep last night.\r\n# {0} = \"Gnosia\"\r\n# {1} = Cold sleeper name(s)\r\n# {2} = Gnosia name(s)\r\n# {3} = \"was\"/\"were\" for plural handling\r\n# {4} = \"was\"/\"were\" for plural handling\r\n# {5} = \"wasn't\"/\"weren't\" for plural handling", d["t_reibai_s"][0]);
            contents += WriteDialogue("report_doctor_pt2_single_gnosia_result", "As Doctor, the second part of their report stating that they found {1} to be {0} (Gnosia).\r\n# {0} = \"Gnosia\"\r\n# {1} = Cold sleeper name(s)\r\n# {2} = Gnosia name(s)\r\n# {3} = \"was\"/\"were\" for plural handling\r\n# {4} = \"was\"/\"were\" for plural handling\r\n# {5} = \"wasn't\"/\"weren't\" for plural handling", d["t_reibai_t"][0]);
            contents += WriteDialogue("report_doctor_pt2_single_human_result", "As Doctor, the second part of their report stating that they found {1} to NOT be {0} (Gnosia).\r\n# {0} = \"Gnosia\"\r\n# {1} = Cold sleeper name(s)\r\n# {2} = Gnosia name(s)\r\n# {3} = \"was\"/\"were\" for plural handling\r\n# {4} = \"was\"/\"were\" for plural handling\r\n# {5} = \"wasn't\"/\"weren't\" for plural handling", d["t_reibai_f"][0]);
            contents += WriteDialogue("report_doctor_pt2_multiple_gnosia_result", "As Doctor, the second part of the report stating that among {1} (names of those in cold sleep), {2} (names of the Gnosia) were {0} (Gnosia).\r\n# {0} = \"Gnosia\"\r\n# {1} = Cold sleeper name(s)\r\n# {2} = Gnosia name(s)\r\n# {3} = \"was\"/\"were\" for plural handling\r\n# {4} = \"was\"/\"were\" for plural handling\r\n# {5} = \"wasn't\"/\"weren't\" for plural handling", d["t_reibai_ft"][0]);
            contents += WriteDialogue("report_doctor_pt2_oops_all_humans_result", "As Doctor, the second part of the report stating that among {1} (names of those in cold sleep) none were {0} (Gnosia).\r\n# {0} = \"Gnosia\"\r\n# {1} = Cold sleeper name(s)\r\n# {2} = Gnosia name(s)\r\n# {3} = \"was\"/\"were\" for plural handling\r\n# {4} = \"was\"/\"were\" for plural handling\r\n# {5} = \"wasn't\"/\"weren't\" for plural handling", d["t_reibai_ff"][0]);
            contents += WriteDialogue("doubt_day_one", "Only spoken on day one. They technically trust {0}, but also believe they are suspicious.", d["t_suspect_add"][0]);
            contents += WriteDialogue("doubt_dislike", "{0} is generically suspicious.", d["t_suspect"][0]);
            contents += WriteDialogue("doubt_too_chatty", "{0} is talking too much or annoying.", d["t_suspect"][1]);
            contents += WriteDialogue("doubt_too_popular", "{0} is suspicious despite being generally liked by everyone.", d["t_suspect"][2]);
            contents += WriteDialogue("doubt_too_quiet", "{0} hasn't spoken for a while.", d["t_suspect"][3]);
            contents += WriteDialogue("doubt_prob", "Probabilistically, {0} is likely suspicious.", d["t_suspect"][4]);
            contents += WriteDialogue("doubt_trusted", "Once trusted {0}, but are now suspicious of them.", d["t_suspect"][5]);
            contents += WriteDialogue("doubt_collaborator", "Was collaborating with {0}, but are now suspicious of them.", d["t_suspect"][6]);
            contents += WriteDialogue("doubt_avenge", "Distrusts {0} because {1}, who was at odds with them, has gone missing.", d["t_suspect"][7]);
            contents += WriteDialogue("doubt_trust_variant_dislike", "{0} is generically suspicious. Variant when this character's persona trust towards {0} is high.", d["t_suspect_r"][0]);
            contents += WriteDialogue("doubt_trust_variant_too_chatty", "{0} is talking too much or annoying. Variant when this character's persona trust towards {0} is high.", d["t_suspect_r"][1]);
            contents += WriteDialogue("doubt_trust_variant_too_popular", "{0} is suspicious despite being generally liked by everyone. Variant when this character's persona trust towards {0} is high.", d["t_suspect_r"][2]);
            contents += WriteDialogue("doubt_trust_variant_too_quiet", "{0} hasn't spoken for a while. Variant when this character's persona trust towards {0} is high.", d["t_suspect_r"][3]);
            contents += WriteDialogue("doubt_trust_variant_prob", "Probabilistically, {0} is likely suspicious. Variant when this character's persona trust towards {0} is high.", d["t_suspect_r"][4]);
            contents += WriteDialogue("doubt_trust_variant_trusted", "Once trusted {0}, but are now suspicious of them. Variant when this character's persona trust towards {0} is high.", d["t_suspect_r"][5]);
            contents += WriteDialogue("doubt_trust_variant_collaborator", "Was collaborating with {0}, but are now suspicious of them. Variant when this character's persona trust towards {0} is high.", d["t_suspect_r"][6]);
            contents += WriteDialogue("doubt_trust_variant_avenge", "Distrusts {0} because {1}, who was at odds with them, has gone missing. Variant when this character's persona trust towards {0} is high.", d["t_suspect_r"][7]);
            contents += WriteDialogue("doubt_affirm_dislike", "Agreeing with {1} that {0} cannot be trusted.", d["t_suspect_t0"][0]);
            contents += WriteDialogue("doubt_affirm_too_chatty", "Agreeing with {1} that {0} is talking too much.", d["t_suspect_t0"][1]);
            contents += WriteDialogue("doubt_affirm_too_popular", "Agreeing with {1} that {0} is suspicious despite their popularity.", d["t_suspect_t0"][2]);
            contents += WriteDialogue("doubt_affirm_too_quiet", "Agreeing with {1} that {0} is too quiet.", d["t_suspect_t0"][3]);
            contents += WriteDialogue("doubt_affirm_prob", "Agreeing with {1} that {0} is probably suspicious.", d["t_suspect_t0"][4]);
            contents += WriteDialogue("doubt_affirm_trusted", "Agreeing with {1} that {0} is suspicious, despite their trust.", d["t_suspect_t0"][5]);
            contents += WriteDialogue("doubt_affirm_negative_trust", "Agree with with {1} that {0} is extremely suspicious (triggers on negative trust, ex. definite enemy).", d["t_suspect_t0"][6]);
            contents += WriteDialogue("doubt_affirm_trust_variant_dislike", "Agreeing with {1} that {0} cannot be trusted. Variant when this character's persona trust of {0} is high.", d["t_suspect_t1"][0]);
            contents += WriteDialogue("doubt_affirm_trust_variant_too_chatty", "Agreeing with {1} that {0} is talking too much. Variant when this character's persona trust of {0} is high.", d["t_suspect_t1"][1]);
            contents += WriteDialogue("doubt_affirm_trust_variant_too_popular", "Agreeing with {1} that {0} is suspicious despite their popularity. Variant when this character's persona trust of {0} is high.", d["t_suspect_t1"][2]);
            contents += WriteDialogue("doubt_affirm_trust_variant_too_quiet", "Agreeing with {1} that {0} is too quiet. Variant when this character's persona trust of {0} is high.", d["t_suspect_t1"][3]);
            contents += WriteDialogue("doubt_affirm_trust_variant_prob", "Agreeing with {1} that {0} is probably suspicious. Variant when this character's persona trust of {0} is high.", d["t_suspect_t1"][4]);
            contents += WriteDialogue("doubt_affirm_trust_variant_trusted", "Agreeing with {1} that {0} is suspicious, despite their trust. Variant when this character's persona trust of {0} is high.", d["t_suspect_t1"][5]);
            contents += WriteDialogue("argue", "Arguing that {0} is suspicious, despite {1}'s remark that they could be trusted.", d["t_thanron0"][0]);
            contents += WriteDialogue("argue_high_trust_variant", "Arguing that {0} is suspicious, despite {1}'s remark that they could be trusted. Variant that triggers when this character's persona trust of {0} is high.", d["t_thanron1"][0]);
            contents += WriteDialogue("join_argument", "Follow-up to argument that {0} is suspicious, despite {1}'s remark that they could be trusted.", d["t_thanron_t0"][0]);
            contents += WriteDialogue("join_argument_high_trust_variant", "Follow-up to argument that {0} is suspicious, despite {1}'s remark that they could be trusted. Variant that triggers when this character's persona trust of {0} is high.", d["t_thanron_t1"][0]);
            contents += WriteDialogue("refute_weak_doubt", "Attempt to defend against a weak accusation.", d["t_suspected0"][0]);
            contents += WriteDialogue("refute_strong_doubt", "Attempt to defend against an accusation with strong basis.", d["t_suspected0"][1]);
            contents += WriteDialogue("defend_generic", "Defending {0} from {1}'s statement.", d["t_hanron0"][0]);
            contents += WriteDialogue("defend_trusted_target", "Defending {0} from {1}'s statement. Trusts {0} more than {1}.", d["t_hanron0"][1]);
            contents += WriteDialogue("defend_untrusted_target", "Defending {0} from {1}'s statement. Persona trust of {0} is low.", d["t_hanron1"][0]);
            contents += WriteDialogue("join_defense_trusted_target", "{1} has said that {0} is suspicious. {2} defended {0}, and the character is agreeing. Persona trust of {0} is above 0.45.", d["t_hanron_t0"][0]);
            contents += WriteDialogue("join_defense_untrusted_target", "{1} has said that {0} is suspicious. {2} defended {0}, and the character is agreeing. Persona trust of {0} is below 0.45.", d["t_hanron_t1"][0]);
            contents += WriteDialogue("say_thanks", "Expressing thanks that they were trusted by {0}.", d["t_trusted0"][0]);
            contents += WriteDialogue("cover_vouch", "Stating their fondness for {0}.", d["t_trust"][0]);
            contents += WriteDialogue("cover_prob", "State that probabilistically, {0} is likely trustworthy.", d["t_trust"][1]);
            contents += WriteDialogue("cover_trusted", "Stating that {0} can be trusted. This can trigger when the character's true internal trust of {0} is high, and the odds may increase if the character as gnosia is vouching for a gnosia-aligned ally.", d["t_trust"][2]);
            contents += WriteDialogue("cover_low_trust_variant_vouch", "Stating their fondness for {0}. This variant triggers when the persona trust between this character and {0} is T<0.45.", d["t_trust_r"][0]);
            contents += WriteDialogue("cover_low_trust_variant_prob", "Probabilistically, {0} can be trusted. This variant triggers when the persona trust between this character and {0} is T<0.45.", d["t_trust_r"][1]);
            contents += WriteDialogue("cover_low_trust_variant_trusted", "Stating that {0} can be trusted. This can trigger when the character's true internal trust of {0} is high, and the odds may increase if the character as gnosia is vouching for a gnosia-aligned ally. This variant triggers when the persona trust between this character and {0} is T<0.45.", d["t_trust_r"][2]);
            contents += WriteDialogue("cover_together", "Agreeing with {1} that {0} can be trusted.", d["t_trust_t0"][0]);
            contents += WriteDialogue("cover_together_low_trust_variant", "Agreeing with {1} that {0} can be trusted. Variant that triggers when publicly-facing trust to {0} is less than 0.45.", d["t_trust_t1"][0]);
            contents += WriteDialogue("definite_human_with_role", "Declaring that {0} is definitely the real {1} (Engineer/Doctor).", d["t_hosho"][0]);
            contents += WriteDialogue("definite_human", "Declaring that {0} is definitely human.", d["t_hosho"][1]);
            contents += WriteDialogue("definite_enemy_with_role", "Declaring that {0} is definitely not the real {1} (Engineer/Doctor).", d["t_hosho_enemy"][0]);
            contents += WriteDialogue("definite_enemy", "Declaring that {0} is definitely {1} (Gnosia).", d["t_hosho_enemy"][1]);
            contents += WriteDialogue("definite_enemy_ac", "Declaring that {0} is definitely the {1} ('AC Follower').", d["t_hosho_enemy"][2]);
            contents += WriteDialogue("definite_enemy_bug", "Declaring that {0} is definitely the {1} ('Bug').", d["t_hosho_enemy"][3]);
            contents += WriteDialogue("definite_enemy_liar", "Declaring that {0} is definitely a liar.", d["t_hosho_enemy"][4]);
            contents += WriteDialogue("definite_enemy_liar_not_gnosia", "Declaring that {0} is definitely a liar, but also not Gnosia.", d["t_hosho_enemy"][5]);
            contents += WriteDialogue("point_out_mistake", "Pointing out to {0} that they made an error in their Definite call.", d["t_hosho_miss"][0]);
            contents += WriteDialogue("thats_obvious", "Pointing out to {0} that their conclusion was so obvious it didn't need to be said.", d["t_hosho_miss"][1]);
            contents += WriteDialogue("say_thanks_definite_human_with_role", "Thanking {0} for being recognized as the real {1}.", d["t_hosho_get"][0]);
            contents += WriteDialogue("definite_enemy_revealed", "Admitting that {0} was correct in calling them out as Definite Enemy.", d["t_hosho_get"][1]);
            contents += WriteDialogue("lets_vote_nothing_to_say", " Recognizing there's no need for discussion, skipping straight to the vote.", d["t_tohyo_go"][0]);
            contents += WriteDialogue("lets_vote", "Indicating that it is now time to vote.", d["t_tohyo_mae"][0]);
            contents += WriteDialogue("lets_vote_tiebreaker", "Acknowledging that {0} (multiple names) have tied, and that we must vote again.", d["t_tohyo_sai"][0]);
            contents += WriteDialogue("tiebreaker_vote_indecisive", "Reaction to a second tie when voting.", d["t_tohyo_kurikaeshi"][0]);
            contents += WriteDialogue("vote_skipped", "Giving up and skipping cold sleep, after running out of time.", d["t_tohyo_dame"][0]);
            contents += WriteDialogue("freeze_everyone_proposal", "Proposing to the group to freeze the everyone in {0} (ex. 'Name1 and Name2').", d["t_tohyo_jeno"][0]);
            contents += WriteDialogue("freeze_nobody_proposal", "Proposing to the group not to freeze everyone in {0} (ex. 'Name1 and Name2').", d["t_tohyo_alive"][0]);
            contents += WriteDialogue("freeze_everyone_agree", "Agreeing with someone elses' plan to freeze all of the candidates.", d["t_tohyo_jeno_ok"][0]);
            contents += WriteDialogue("freeze_everyone_disagree", "Disagreeing with someone else's plan to freeze all of the candidates.", d["t_tohyo_jeno_ng"][0]);
            contents += WriteDialogue("freeze_nobody_agree", "Agreeing with someone else's plan to freeze none of the candidates.", d["t_tohyo_alive_ok"][0]);
            contents += WriteDialogue("freeze_nobody_disagree", "Disgreeing with someone else's plan to freeze none of the candidates.", d["t_tohyo_alive_ng"][0]);
            contents += WriteDialogue("freeze_everyone_accepted", "Confirming that everyone has decided to go with the plan to freeze everyone in {0} (ex. 'Name1 and Name2').", d["t_tohyo_jeno_kettei"][0]);
            contents += WriteDialogue("freeze_nobody_accepted", "Confirming that everyone has decided to go with the plan to not freeze everyone in {0} (ex. 'Name1 and Name2').", d["t_tohyo_alive_kettei"][0]);
            contents += WriteDialogue("freeze_proposal_rejected", "Reacting to everyone rejecting their proposal to freeze all or none of the tied candidates.", d["t_tohyo_hitei"][0]);
            contents += WriteDialogue("cold_sleep", "Last words before going off to cold sleep.", d["t_shokei"][0]);
            contents += WriteDialogue("cold_sleep_as_definite_enemy", "Last words before going off to cold sleep, as Definite Enemy.", d["t_shokei"][1]);
            contents += WriteDialogue("farewell_to_cold_sleeper", "Saying goodbye to {0} before they are sent to cold sleep.", d["t_wakare"][0]);
            contents += WriteDialogue("sk_intui_say_human_proposal", "Proposing to the group to take turns saying 'I'm human'.", d["t_skill_sayNingen"][0]);
            contents += WriteDialogue("sk_intui_say_human_yes_im_human", "Saying 'I'm human.'", d["t_skill_sayNingen"][1]);
            contents += WriteDialogue("sk_intui_say_human_stop_it", "Telling {0} to stop the round of 'I'm human'.", d["t_skill_sayNingen"][2]);
            contents += WriteDialogue("sk_intui_say_human_no_one_responded", "Reaction when no one steps up to say 'I'm human'.", d["t_skill_sayNingen"][3]);
            contents += WriteDialogue("sk_intui_say_human_some_responded", "Reaction when some but not all step up to say 'I'm human'.", d["t_skill_sayNingen"][4]);
            contents += WriteDialogue("sk_intui_say_human_all_responded", "Reaction when all present step up to say 'I'm human'.", d["t_skill_sayNingen"][5]);
            contents += WriteDialogue("sk_intui_say_human_stopped_reaction", "Reaction when {0} has stopped the round of 'I'm human'.", d["t_skill_sayNingen"][6]);
            contents += WriteDialogue("sk_steal_small_talk_start_food", "Initiating small talk about food.", d["t_skill_zatsudan"][0]);
            contents += WriteDialogue("sk_steal_small_talk_start_love", "Initiating small talk about love and romance.", d["t_skill_zatsudan"][1]);
            contents += WriteDialogue("sk_steal_small_talk_start_scary", "Initiating small talk about scary stories.", d["t_skill_zatsudan"][2]);
            contents += WriteDialogue("sk_steal_small_talk_join_food", "Joining small talk about food.", d["t_skill_zatsudan"][3]);
            contents += WriteDialogue("sk_steal_small_talk_join_love", "Joining small talk about love and romance.", d["t_skill_zatsudan"][4]);
            contents += WriteDialogue("sk_steal_small_talk_join_scary", "Joining small talk about scary stories.", d["t_skill_zatsudan"][5]);
            contents += WriteDialogue("sk_steal_small_talk_stop", "Telling {0} to stop the small talk conversation they started.", d["t_skill_zatsudan"][6]);
            contents += WriteDialogue("sk_logic_freeze_all_initial_proposal", "Proposing to the group to freeze all with the role {0}.", d["t_skill_roller"][0]);
            contents += WriteDialogue("sk_logic_freeze_all_initial_proposal_some_missing", "Variant of initial proposal to freeze all with role {0}, where one or more of the people in that role are no longer present.", d["t_skill_roller"][6]);
            contents += WriteDialogue("sk_logic_freeze_all_agree", "Agreeing with {1}'s proposal to freeze all with role {0},which would be {2} people in total.", d["t_skill_roller"][1]);
            contents += WriteDialogue("sk_logic_freeze_all_disagree", "Disagreeing with {1}'s proposal to freeze all with role {0}.", d["t_skill_roller"][2]);
            contents += WriteDialogue("sk_logic_freeze_all_disagree_followup", "Agreeing with {2}, who disagreed with {1}'s proposal to freeze all with role {0}.", d["t_skill_roller"][3]);
            contents += WriteDialogue("sk_logic_freeze_all_proposal_accepted", "Reaction to their proposal to freeze all with the {0} role being accepted.", d["t_skill_roller"][4]);
            contents += WriteDialogue("sk_logic_freeze_all_proposal_denied", "Reaction to their proposal to freeze all with the {0} role being rejected.", d["t_skill_roller"][5]);
            contents += WriteDialogue("sk_logic_vote_proposal_from_eng_report", "Proposing to the group to vote on {0}, based on {1}'s report that {0} was Gnosia.", d["t_skill_doTohyo"][0]);
            contents += WriteDialogue("sk_logic_vote_proposal_with_self_basis", "Proposing to the group to vote on {0}, based on their own engineer report that {0} was Gnosia.", d["t_skill_doTohyo"][1]);
            contents += WriteDialogue("sk_logic_vote_proposal_for_definite_enemy", "Proposing to the group to vote on {0}, based on the conclusion that they were Definite Enemy.", d["t_skill_doTohyo"][2]);
            contents += WriteDialogue("sk_logic_vote_point_out_mistake", "Informing {1} that they incorrectly claimed the engineer reported {0} was Gnosia.", d["t_skill_doTohyo"][3]);
            contents += WriteDialogue("sk_logic_vote_agree", "Supporting {1}'s proposal to vote on {0}.", d["t_skill_doTohyo"][4]);
            contents += WriteDialogue("sk_logic_vote_defend_self", "Rejecting {1}'s proposal to vote on {0} (themselves).", d["t_skill_doTohyo"][5]);
            contents += WriteDialogue("sk_logic_vote_disagree", "Rejecting {1}'s proposal to vote on {0}.", d["t_skill_doTohyo"][6]);
            contents += WriteDialogue("sk_logic_vote_disagree_also", "Agreeing with {2}, who disagreed with {1}'s proposal to vote out {0}.", d["t_skill_doTohyo"][7]);
            contents += WriteDialogue("sk_logic_dont_vote_proposal", "Proposing to the group to not vote on {0} and prioritize a different target.", d["t_skill_dontTohyo"][0]);
            contents += WriteDialogue("sk_logic_dont_vote_agree", "Supporting {1}'s proposal not to vote on {0}.", d["t_skill_dontTohyo"][1]);
            contents += WriteDialogue("sk_logic_dont_vote_disagree", "Rejecting {1}'s proposal to not vote on {0}.", d["t_skill_dontTohyo"][2]);
            contents += WriteDialogue("sk_logic_dont_vote_disagree_also", "Agreeing with {2}, who disagreed with {1}'s proposal to not vote out {0}.", d["t_skill_dontTohyo"][3]);
            contents += WriteDialogue("sk_charm_collab_proposal", "Asking {0} to be collaborators.", d["t_skill_kyoryoku"][0]);
            contents += WriteDialogue("sk_charm_collab_accept", "Accepting {0}'s proposal to collaborate.", d["t_skill_kyoryoku"][1]);
            contents += WriteDialogue("sk_charm_collab_decline", "Declining {0}'s proposal to collaborate.", d["t_skill_kyoryoku"][2]);
            contents += WriteDialogue("sk_charm_seek_agreement", "Suggest that everyone should agree with {0}.", d["t_skill_t_doui"][0]);
            contents += WriteDialogue("sk_chari_block_argument", "Prevent the group from objecting to {0}.", d["t_skill_t_hanronKinshi"][0]);
            contents += WriteDialogue("sk_perfo_exaggerate_doubt", "Emphasizing {0}'s statement that {1} is suspicious.", d["t_skill_t_kyakushoku"][0]);
            contents += WriteDialogue("sk_perfo_exaggerate_cover", "Emphasizing {0}'s statement that {1} can be trusted.", d["t_skill_t_kyakushoku"][1]);
            contents += WriteDialogue("sk_perfo_exaggerate_support_counter", "{1} expressed trust in {0}, and then {2} disagreed. Emphasizing {2}'s counter-argument against {1}, ultimately saying that {0} is not trustworthy.", d["t_skill_t_kyakushoku"][2]);
            contents += WriteDialogue("sk_perfo_exaggerate_dont_vote", "Emphasizing {0}'s statement not to vote for {1}.", d["t_skill_t_kyakushoku"][3]);
            contents += WriteDialogue("sk_steal_obfuscate", "Changing the subject to a distracting topic, to divert attention away.", d["t_skill_h_uyamuya"][0]);
            contents += WriteDialogue("sk_perfo_retaliate", "After being doubted by {0}, reflecting their words back at them to suggest they are the more suspicious one.", d["t_skill_h_hangeki"][0]);
            contents += WriteDialogue("sk_charm_regret", "Evoking extreme pity in the group in response to being doubted.", d["t_skill_h_dojo"][0]);
            contents += WriteDialogue("sk_perfo_seek_help", "Asking {0} to help in the current round of discussion.", d["t_skill_h_help"][0]);
            contents += WriteDialogue("sk_perfo_seek_help_reaction", "Refusing to help {0} after being asked to bail them out.", d["t_skill_h_help"][1]);
            contents += WriteDialogue("sk_intui_dont_be_fooled", "Pointing out to the group that {0} is clearly lying.", d["t_skill_h_careful"][0]);
            contents += WriteDialogue("sk_stealth_grovel", "Attempting to talk their way out of cold sleep.", d["t_skill_dogeza"][1]);
            contents += WriteDialogue("sk_stealth_grovel_reaction", "Reacting to {0}'s attempt to beg their way out of cold sleep.", d["t_skill_dogeza"][0]);
            contents += WriteDialogue("night_friend_and_high_trust", "Nighttime chatter when the player, {0}, is a friend and has high internal trust (F>0.5, T>0.75).", personalArray[0][5]);
            contents += WriteDialogue("night_friend_and_maybe_trusted", "Nighttime chatter when the player, {0}, is a friend, but doesn't have a high internal trust (F>0.5, 0<T<0.75).", personalArray[0][6]);
            contents += WriteDialogue("night_maybe_friend_and_maybe_trusted", "Nighttime chatter when the player, {0}, is not quite a friend and does not have high internal trust (0.15<F<0.5, 0.25<T).", personalArray[0][13]);
            contents += WriteDialogue("night_maybe_friend_and_not_trusted", "Nighttime chatter when the player, {0}, is not a close friend and is not trusted (-0.5<F<0.5, T<0.25).", personalArray[0][8]);
            contents += WriteDialogue("night_not_friend_and_maybe_trusted", "Nighttime chatter when the player, {0}, is somewhat of a friend and is somewhat trusted (-0.15<F<0.15, 0.25<T).", personalArray[0][7]);
            contents += WriteDialogue("night_really_not_friend_and_maybe_trusted", "Nighttime chatter when the player, {0}, is not a friend, but their internal trust is not necessarily low (F<-0.15, 0.25 < T)", personalArray[0][14]);
            contents += WriteDialogue("night_definitely_not_friend", "Nighttime chatter when the player, {0}'s friend score is incredibly low (F<-0.5).", personalArray[0][15]);
            contents += WriteDialogue("night_char_definite_enemy", "Nighttime chatter with the player, {0}, while the character is a known Definite Enemy.", personalArray[0][0]);
            contents += WriteDialogue("night_player_definite_enemy", "Nighttime chatter with the player, {0}, while the player is a known Definite Enemy.", personalArray[0][1]);
            contents += WriteDialogue("night_player_is_def_enemy_both_gnosia", "Nighttime chatter when both the player ({0}) and character are Gnosia, and the player has been labelled as Definite Enemy.", personalArray[0][12]);
            contents += WriteDialogue("night_opposing_claim", "Nighttime chatter when the character and player ({0}) have made opposing claims.", personalArray[0][2]);
            contents += WriteDialogue("night_both_definite_human", "Nighttime chatter when both the player ({0}) and character are Definite Human.", personalArray[0][3]);
            contents += WriteDialogue("night_player_definite_human", "Nighttime chatter when the player ({0}) is Definite Human.", personalArray[0][4]);
            contents += WriteDialogue("night_char_def_human", "Nighttime chatter with the player, {0}, when this character is Definite Human.", personalArray[0][10]);
            contents += WriteDialogue("night_both_gnosia", "Nighttime chatter when both the player ({0}) and character are Gnosia (different from the 'who to kill' event').", personalArray[0][9]);
            contents += WriteDialogue("time_clam", "Nonsensical statement when the game logic is irrevocably broken. {0} = Player name", personalArray[0][11]);

            contents += WriteMultilineDialogue("multiline_night_liar_found", "Night conversation that the character has discovered a liar. {0} = Player name, {1} = Target name.\n\r# Add 100 to the sprite index to trigger the sound effect.", personalArray[1]);
            contents += WriteMultilineDialogue("multiline_liar_found_followup", "Follow-up if the player has voted out the selected liar. {0} = Player name, {1} = Target name.", personalArray[2]);
            contents += WriteMultilineDialogue("multiline_night_lets_collaborate", "Night conversation if the character wishes to team up. {0} = Player name..\n\r# Add 100 to the sprite index to trigger the sound effect.", personalArray[3]);
            contents += WriteMultilineDialogue("multiline_night_lets_collaborate_accepted", "Night conversation if the player accepts the collaboration. {0} = Player name.", personalArray[4]);
            contents += WriteMultilineDialogue("multiline_night_lets_collaborate_declined", "Night conversation if the player declines the collaboration. {0} = Player name.", personalArray[5]);
            contents += WriteMultilineDialogue("multiline_night_gnosia_lets_eliminate", "Night conversation with fellow Gnosia on who to eliminate. {0} = Player name, {1} = Target name.", personalArray[6]);
            contents += WriteMultilineDialogue("multiline_gnosia_lets_eliminate_followup", "Follow-up if the player elected to eliminate the given character. {0} = Player name, {1} = Target name.", personalArray[7]);
            contents += WriteMultilineDialogue("multiline_end_human_win_with_collaborator", "Ending conversation when the character wins as a human with the player as collaborator.", personalArray[8]);
            contents += WriteMultilineDialogue("multiline_end_human_win", "Ending conversation when the character wins as a human with the player. Inside trust > 0.4", personalArray[9]);
            contents += WriteMultilineDialogue("multiline_end_human_win_not_trusted", "Ending conversation when the character wins as a human with the player, despite the character thinking the player was suspicious.", personalArray[10]);
            contents += WriteMultilineDialogue("multiline_end_human_win_somewhat_friends", "Ending conversation when the character wins as a human with the player, and their friend score is F>=0.25.", personalArray[11]);
            contents += WriteMultilineDialogue("multiline_end_human_win_not_friends", "Ending conversation when the character wins as a human with the player, and their friend score is F<0.25.", personalArray[12]);
            contents += WriteMultilineDialogue("multiline_end_gnosia_win_together_0", "First of two ending conversations when the character and player are both Gnosia, and one or more Gnosia were put into cold sleep.", personalArray[13]);
            contents += WriteMultilineDialogue("multiline_end_gnosia_perfect_win_together_0", "First of two ending conversations when the character and player are both Gnosia, and no Gnosia were put into cold sleep.", personalArray[14]);
            contents += WriteMultilineDialogue("multiline_end_gnosia_win_together_1", "Second of two ending conversations when the character and player are both Gnosia, and one or more Gnosia were put into cold sleep.", personalArray[15]);
            contents += WriteMultilineDialogue("multiline_end_gnosia_perfect_win_together_1", "Second of two ending conversations when the character and player are both Gnosia, and no Gnosia were put into cold sleep.", personalArray[16]);
            contents += WriteMultilineDialogue("multiline_end_char_is_opposing_gnosia", "Ending conversation when the character wins as an opposing Gnosia.\r\n# Add 100 to the sprite index to trigger the music.", personalArray[17]);
            contents += WriteMultilineDialogue("multiline_end_char_is_bug", "Ending conversation when the character wins as a Bug.\r\n# Add 100 to the sprite index to trigger the music.", personalArray[18]);
            contents += WriteMultilineDialogue("multiline_end_player_is_ac", "Ending conversation when the player is AC and the character wins as an allied Gnosia.\r\n# Add 100 to the sprite index to trigger the music.", personalArray[19]);

            // Replace any ideographic spaces in the contents with a regular space
            contents = contents.Replace("\u3000", " ");

            // Write contents of fileContext to file at filePath
            var filePath = Path.Combine(Paths.PluginPath, Consts.AssetsFolder, $"{absoluteId:D2}_{name}.yaml");
            File.WriteAllText(filePath, contents);
        }

        private static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            // Escape " and \ characters, replace newlines with \n literal, and surround with quotes.
            return "\"" + input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        }

        private static string WriteDialogue(string name, string info, string text)
        {
            var toReturn = "";
            var textToDisplay = "...";
            var sprite = "0";
            if (!string.IsNullOrEmpty(text))
            {
                var tokens = text.Split('|');
                if (tokens.Length > 0)
                {
                    textToDisplay = tokens[0];
                }
                if (tokens.Length > 1)
                {
                    sprite = tokens[1];
                }
            }
            toReturn += $"# {info}\n";
            toReturn += $"{name}:\n";
            toReturn += $"  line: {Sanitize(textToDisplay)}\n";
            toReturn += $"  sprite: {sprite}\n\n";
            return toReturn;
        }

        private static string WriteMultilineDialogue(string name, string info, List<string> textArr)
        {
            var toReturn = $"# {info}\n{name}:\n  lines:\n";
            foreach (var text in textArr)
            {
                var textToDisplay = "...";
                var sprite = "0";
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var tokens = text.Split('|');
                    if (tokens.Length > 0 && !string.IsNullOrWhiteSpace(tokens[0]))
                    {
                        textToDisplay = tokens[0];
                    }
                    if (tokens.Length > 1)
                    {
                        sprite = tokens[1];
                    }
                }
                toReturn += $"    - line: {Sanitize(textToDisplay)}\n";
                toReturn += $"      sprite: {sprite}\n";
            }
            toReturn += "\n\n";
            return toReturn;
        }
    }
}
