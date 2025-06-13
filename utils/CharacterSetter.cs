using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace GnosiaCustomizer.utils
{
    internal class CharacterSetter
    {
        internal static readonly Type DataType = AccessTools.TypeByName("gnosia.Data");
        internal static readonly Type CharaDataType = AccessTools.Inner(DataType, "CharaData");
        internal static readonly FieldInfo CharaField = AccessTools.Field(DataType, "Chara");

        public const string SubstitutionPrefix = "gc%";
        public const char Delimiter = '%';
        private const string NameFieldName = "name";
        private const string SexFieldName = "sex";
        private const string AgeFieldName = "age";
        private const string OriginFieldName = "d_place";
        private const string HonorificFieldName = "t_keisho";
        private const string DefenseMinFieldName = "hpMin";
        private const string DefenseWithGnosFieldName = "hpWithGnos";
        private const string PersonalFieldName = "t_personal";
        private const int PersonalArrayLength = 20;

        internal static readonly Dictionary<string, List<string>> DialogueInitialization = new Dictionary<string, List<string>>()
        {
            { "t_aisatu", [ "introduction" ] },
            { "t_suspect", [ "doubt_dislike%{0}", "doubt_too_chatty%{0}", "doubt_too_popular%{0}", "doubt_too_quiet%{0}", "doubt_prob%{0}", "doubt_trusted%{0}", "doubt_collaborator%{0}", "doubt_avenge%{0}" ] },
            { "t_suspect_r", [ "doubt_trust_variant_dislike%{0}", "doubt_trust_variant_too_chatty%{0}", "doubt_trust_variant_too_popular%{0}", "doubt_trust_variant_too_quiet%{0}", "doubt_trust_variant_prob%{0}", "doubt_trust_variant_trusted%{0}", "doubt_trust_variant_collaborator%{0}", "doubt_trust_variant_avenge%{0}" ] },
            { "t_suspect_add", [ "doubt_day_one%{0}" ] },
            { "t_suspect_t0", [ "doubt_affirm_dislike%{0}%{1}", "doubt_affirm_too_chatty%{0}%{1}",  "doubt_affirm_too_popular%{0}%{1}", "doubt_affirm_too_quiet%{0}%{1}", "doubt_affirm_prob%{0}%{1}", "doubt_affirm_trusted%{0}%{1}", "doubt_affirm_negative_trust%{0}%{1}" ]},
            { "t_suspect_t1", [ "doubt_affirm_trust_variant_dislike%{0}%{1}", "doubt_affirm_trust_variant_too_chatty%{0}%{1}", "doubt_affirm_trust_variant_too_popular%{0}%{1}", "doubt_affirm_trust_variant_too_quiet%{0}%{1}", "doubt_affirm_trust_variant_prob%{0}%{1}", "doubt_affirm_trust_variant_trusted%{0}%{1}" ] },
            { "t_suspected0", [ "refute_weak_doubt", "refute_strong_doubt"] },
            { "t_hanron0", [ "defend_generic%{0}%{1}", "defend_trusted_target%{0}%{1}"] },
            { "t_hanron1", [ "defend_untrusted_target%{0}%{1}" ] },
            { "t_hanron_t0", [ "join_defense_trusted_target%{0}%{1}%{2}" ] },
            { "t_hanron_t1", [ "join_defense_untrusted_target%{0}%{1}%{2}" ] },
            { "t_trust", [ "cover_vouch%{0}", "cover_prob%{0}", "cover_trusted%{0}"] },
            { "t_trust_r", [ "cover_low_trust_variant_vouch%{0}", "cover_low_trust_variant_prob%{0}", "cover_low_trust_variant_trusted%{0}" ] },
            { "t_trust_t0", [ "cover_together%{0}%{1}" ] },
            { "t_trust_t1", [ "cover_together_low_trust_variant%{0}%{1}" ] },
            { "t_trusted0", [ "say_thanks%{0}" ] },
            { "t_thanron0", [ "argue%{0}%{1}" ] },
            { "t_thanron1", [ "argue_high_trust_variant%{0}%{1}" ] },
            { "t_thanron_t0", ["join_argument%{0}%{1}"] },
            { "t_thanron_t1", ["join_argument_high_trust_variant%{0}%{1}"] },
            { "t_hosho", ["definite_human_with_role%{0}%{1}", "definite_human%{0}%{1}"] },
            { "t_hosho_enemy", ["definite_enemy_with_role%{0}%{1}", "definite_enemy%{0}%{1}", "definite_enemy_ac%{0}%{1}", "definite_enemy_bug%{0}%{1}", "definite_enemy_liar%{0}%{1}", "definite_enemy_liar_not_gnosia%{0}%{1}"] },
            { "t_hosho_miss", ["point_out_mistake%{0}", "thats_obvious%{0}"] },
            { "t_hosho_get", ["say_thanks_definite_human_with_role%{0}%{1}", "definite_enemy_revealed%{0}"] },
            { "t_tohyo_go", ["lets_vote_nothing_to_say"] },
            { "t_tohyo_mae", ["lets_vote"] },
            { "t_tohyo_sai", ["lets_vote_tiebreaker%{0}"] },
            { "t_shokei", ["cold_sleep", "cold_sleep_as_definite_enemy"] },
            { "t_wakare", ["farewell_to_cold_sleeper%{0}"] },
            { "t_tohyo_kurikaeshi", ["tiebreaker_vote_indecisive"] },
            { "t_tohyo_jeno", ["freeze_everyone_proposal%{0}"] },
            { "t_tohyo_alive", ["freeze_nobody_proposal%{0}"] },
            { "t_tohyo_jeno_ok", ["freeze_everyone_agree"] },
            { "t_tohyo_jeno_ng", ["freeze_everyone_disagree"] },
            { "t_tohyo_alive_ok", ["freeze_nobody_agree"] },
            { "t_tohyo_alive_ng", ["freeze_nobody_disagree"] },
            { "t_tohyo_jeno_kettei", ["freeze_everyone_accepted%{0}"] },
            { "t_tohyo_alive_kettei", ["freeze_nobody_accepted%{0}"] },
            { "t_tohyo_hitei", ["freeze_proposal_rejected"] },
            { "t_tohyo_dame", ["freeze_proposal_indecisive"] },
            { "t_co", ["", "reveal_role_engineer%{0}", "reveal_role_doctor%{0}", "reveal_role_guardian_angel%{0}", "reveal_role_guard_duty%{0}"] },
            { "t_co_find", ["", "reveal_role_engineer_with_gnosia_result%{0}%{1}", "reveal_role_doctor_with_gnosia_result%{0}%{1}"] },
            { "t_co_taiko", ["", "reveal_own_role_engineer%{0}%{1}", "reveal_own_role_doctor%{0}%{1}", "", "reveal_role_guard_duty_response%{0}%{1}"] },
            { "t_co_req", ["step_forward%{0}"] },
            { "t_co_after", ["step_forward_1_claim_reaction%{0}%{1}", "step_forward_2_claims_reaction%{0}%{1}", "step_forward_3_claims_reaction%{0}%{1}"] },
            { "t_uranai_o", ["report_engineer_pt1_target_killed%{0}%{1}%"] },
            { "t_uranai_s", ["report_engineer_pt1%{0}%{1}%"] },
            { "t_uranai_b", ["report_engineer_pt1_different_target%{0}%{1}%"] },
            { "t_uranai_t", ["report_engineer_pt2_gnosia_result%{0}%{1}"] },
            { "t_uranai_f", ["report_engineer_pt2_human_result%{0}%{1}"] },
            { "t_uranai_n", ["report_engineer_no_result"] },
            { "t_reibai_s", ["report_doctor_pt1%{0}%{1}%{2}%{3}%{4}%{5}%"] },
            { "t_reibai_t", ["report_doctor_pt2_single_gnosia_result%{0}%{1}%{2}%{3}%{4}%{5}"] },
            { "t_reibai_f", ["report_doctor_pt2_single_human_result%{0}%{1}%{2}%{3}%{4}%{5}"] },
            { "t_reibai_ft", ["report_doctor_pt2_multiple_gnosia_result%{0}%{1}%{2}%{3}%{4}%{5}"] },
            { "t_reibai_ff", ["report_doctor_pt2_oops_all_humans_result%{0}%{1}%{2}%{3}%{4}%{5}"] },
            { "t_houkoku_s", ["report_pt1_matching_result%{0}%{1}%"] },
            { "t_houkoku_w", ["report_pt1_conflicting_result%{0}%{1}%"] },
            { "t_okuyami", ["opening_remarks_condolences%{0}"] },
            { "t_okuyami_n", ["opening_remarks_no_deaths"] },
            { "t_okuyami_f", ["opening_remarks_two_deaths%{0}"] },
            { "t_houkoku_req", ["request_report"] },
            { "t_houkoku_not", ["request_report_no_response%{0}"] },
            { "t_skill_sayNingen", ["sk_intui_say_human_proposal", "sk_intui_say_human_yes_im_human", "sk_intui_say_human_stop_it%{0}", "sk_intui_say_human_no_one_responded", "sk_intui_say_human_some_responded", "sk_intui_say_human_all_responded", "sk_intui_say_human_stopped_reaction%{0}"] },
            { "t_skill_zatsudan", ["sk_steal_small_talk_start_food", "sk_steal_small_talk_start_love", "sk_steal_small_talk_start_scary", "sk_steal_small_talk_join_food", "sk_steal_small_talk_join_love", "sk_steal_small_talk_join_scary", "sk_steal_small_talk_stop%{0}"] },
            { "t_skill_roller", ["sk_logic_freeze_all_initial_proposal%{0}", "sk_logic_freeze_all_agree%{0}%{1}%{2}", "sk_logic_freeze_all_disagree%{0}%{1}", "sk_logic_freeze_all_disagree_followup%{0}%{1}%{2}", "sk_logic_freeze_all_proposal_accepted%{0}", "sk_logic_freeze_all_proposal_denied%{0}", "sk_logic_freeze_all_initial_proposal_some_missing%{0}" ] },
            { "t_skill_doTohyo", ["sk_logic_vote_proposal_from_eng_report%{0}%{1}", "sk_logic_vote_proposal_with_self_basis%{0}", "sk_logic_vote_proposal_for_definite_enemy%{0}", "sk_logic_vote_point_out_mistake%{0}%{1}", "sk_logic_vote_agree%{0}", "sk_logic_vote_defend_self%{0}%{1}", "sk_logic_vote_disagree%{0}%{1}", "sk_logic_vote_disagree_also%{0}%{1}%{2}"] },
            { "t_skill_dontTohyo", ["sk_logic_dont_vote_proposal%{0}", "sk_logic_dont_vote_agree%{0}%{1}", "sk_logic_dont_vote_disagree%{0}%{1}", "sk_logic_dont_vote_disagree_also%{0}%{1}%{2}"] },
            { "t_skill_kyoryoku", ["sk_charm_collab_proposal%{0}", "sk_charm_collab_accept%{0}", "sk_charm_collab_decline%{0}"] },
            { "t_skill_t_doui", ["sk_charm_seek_agreement%{0}"] },
            { "t_skill_t_hanronKinshi", ["sk_chari_block_argument%{0}"] },
            { "t_skill_t_kyakushoku", ["sk_perfo_exaggerate_doubt%{0}%{1}", "sk_perfo_exaggerate_cover%{0}%{1}", "sk_perfo_exaggerate_support_counter%{0}%{1}", "sk_perfo_exaggerate_dont_vote%{0}%{1}"] },
            { "t_skill_h_uyamuya", ["sk_steal_obfuscate"] },
            { "t_skill_h_hangeki", ["sk_perfo_retaliate%{0}"] },
            { "t_skill_h_dojo", ["sk_charm_regret"] },
            { "t_skill_h_help", ["sk_perfo_seek_help%{0}", "sk_perfo_seek_help_reaction%{0}"] },
            { "t_skill_h_careful", ["sk_intui_dont_be_fooled%{0}"] },
            { "t_skill_dogeza", ["sk_stealth_grovel_reaction%{0}", "sk_stealth_grovel%{0}"] },
            { "t_temp", ["bio1", "bio2"] }
        };

        private static readonly List<string> PersonalLines0 = new List<string>
        {
            "night_char_definite_enemy%{0}",
            "night_player_definite_enemy%{0}",
            "night_opposing_claim%{0}",
            "night_both_definite_human%{0}",
            "night_player_definite_human%{0}",
            "night_friend_and_high_trust%{0}",
            "night_friend_and_maybe_trusted%{0}",
            "night_not_friend_and_maybe_trusted%{0}",
            "night_maybe_friend_and_not_trusted%{0}",
            "night_both_gnosia%{0}",
            "night_char_def_human%{0}",
            "time_clam",
            "night_player_is_def_enemy_both_gnosia%{0}",
            "night_maybe_friend_and_maybe_trusted%{0}",
            "night_really_not_friend_and_maybe_trusted%{0}",
            "night_definitely_not_friend%{0}",
            "opening_statement",
        };

        private static readonly List<string> PersonalLines1AndUp = new List<string>
        {
            "multiline_night_liar_found%{0}%{1}",
            "multiline_liar_found_followup%{0}%{1}",
            "multiline_night_lets_collaborate%{0}",
            "multiline_night_lets_collaborate_accepted%{0}",
            "multiline_night_lets_collaborate_declined%{0}",
            "multiline_night_gnosia_lets_eliminate%{0}%{1}",
            "multiline_gnosia_lets_eliminate_followup%{0}%{1}",
            "multiline_end_human_win_with_collaborator%{0}",
            "multiline_end_human_win%{0}",
            "multiline_end_human_win_not_trusted%{0}",
            "multiline_end_human_win_somewhat_friends%{0}",
            "multiline_end_human_win_not_friends%{0}",
            "multiline_end_gnosia_win_together_0%{0}",
            "multiline_end_gnosia_perfect_win_together_0%{0}",
            "multiline_end_gnosia_win_together_1%{0}",
            "multiline_end_gnosia_perfect_win_together_1%{0}",
            "multiline_end_char_is_opposing_gnosia%{0}",
            "multiline_end_char_is_bug%{0}",
            "multiline_end_player_is_ac%{0}"
        };

        // Gets the value for the given field name for the given character absolute id
        internal static bool GetCharaFieldValue(int absoluteId, string fieldName, out object value)
        {
            var charaArray = (Array)CharaField.GetValue(null);
            if (charaArray == null || absoluteId < 0 || absoluteId >= charaArray.Length)
                throw new IndexOutOfRangeException($"Invalid CharaData index: {absoluteId}");

            var field = AccessTools.Field(CharaDataType, fieldName);
            if (field == null)
            {
                value = null;
                return false;
            }
            value = field.GetValue(charaArray.GetValue(absoluteId));
            return true;
        }

        // Retrieves the given field for the given character absolute id as a string
        internal static bool GetCharaFieldValueAsString(int absoluteId, string fieldName, out string value)
        {
            if (GetCharaFieldValue(absoluteId, fieldName, out var charaValue) && charaValue is string strValue)
            {
                value = strValue;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        // Retrieves the given field for the given character absolute id as a List<string>
        internal static bool GetCharaFieldValueAsStringArray(int absoluteId, string fieldName, out List<string> strArray)
        {
            if (GetCharaFieldValue(absoluteId, fieldName, out var value) && value is List<string> stringList)
            {
                strArray = stringList;
                return true;
            }
            else
            {
                strArray = null;
                return false;
            }
        }

        // Retrieves the given field for the given character absolute id as a List<List<string>>
        internal static bool GetCharaFieldAs2dStringArray(int absoluteId, string fieldName, out List<List<string>> strArray)
        {
            if (GetCharaFieldValue(absoluteId, fieldName, out var value) && value is List<List<string>> string2dList)
            {
                strArray = string2dList;
                return true;
            }
            else
            {
                strArray = null;
                return false;
            }
        }

        // Sets the value of a field in the CharaData struct
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

        internal static void SetChara(ManualLogSource Logger, int index, CharacterText charaText)
        {
            //Logger.LogInfo($"Setting character data for index {index}");
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
            var charaStructBoxed = array.GetValue(index);

            if (charaText.Name != null)
            {
                //Logger.LogInfo($"Setting character name to: {charaText.Name}");
                SetField(charaStructBoxed, NameFieldName, charaText.Name);
            }
            if (charaText.Sex != null)
            {
                SetField(charaStructBoxed, SexFieldName, charaText.Sex.Value);
            }
            if (charaText.Origin != null)
            {
                SetField(charaStructBoxed, OriginFieldName, charaText.Origin);
            }
            if (charaText.Age != null)
            {
                SetField(charaStructBoxed, AgeFieldName, charaText.Age.Value);
            }
            if (charaText.Honorific != null)
            {
                SetField(charaStructBoxed, HonorificFieldName, charaText.Honorific);
            }
            if (charaText.DefenseMin != null)
            {
                SetField(charaStructBoxed, DefenseMinFieldName, charaText.DefenseMin.Value);
            }
            if (charaText.DefenseWithGnos != null)
            {
                SetField(charaStructBoxed, DefenseWithGnosFieldName, charaText.DefenseWithGnos.Value);
            }
            if (charaText.Attributes != null)
            {
                var attr = charaText.Attributes;
                if (attr.TryGetValue("playful", out var playful)
                    && attr.TryGetValue("social", out var social)
                    && attr.TryGetValue("logic", out var logic)
                    && attr.TryGetValue("neat", out var neat)
                    && attr.TryGetValue("desire", out var desire)
                    && attr.TryGetValue("courage", out var courage))
                {
                    SetField(charaStructBoxed, "attr", new List<float>()
                    {
                        playful, social, logic, neat, desire, courage
                    });
                }
            }

            if (charaText.AbilityStart != null)
            {
                var abil = charaText.AbilityStart;
                if (abil.TryGetValue("charisma", out var charisma)
                    && abil.TryGetValue("intuition", out var intuition)
                    && abil.TryGetValue("charm", out var charm)
                    && abil.TryGetValue("logic", out var logic)
                    && abil.TryGetValue("perform", out var perform)
                    && abil.TryGetValue("stealth", out var stealth))
                {
                    SetField(charaStructBoxed, "abil", new List<float>()
                    {
                        charisma, intuition, charm, logic, perform, stealth
                    });
                }
            }

            if (charaText.AbilityMax != null)
            {
                var abil = charaText.AbilityMax;
                if (abil.TryGetValue("charisma", out var charisma)
                    && abil.TryGetValue("intuition", out var intuition)
                    && abil.TryGetValue("charm", out var charm)
                    && abil.TryGetValue("logic", out var logic)
                    && abil.TryGetValue("perform", out var perform)
                    && abil.TryGetValue("stealth", out var stealth))
                {
                    SetField(charaStructBoxed, "abilMax", new List<float>()
                    {
                        charisma, intuition, charm, logic, perform, stealth
                    });
                }
            }

            // Replace dialogue with placeholders
            foreach (var fieldName in DialogueInitialization.Keys)
            {
                // Get the original dialogue
                var toAdd = new List<string>(DialogueInitialization[fieldName].Count);
                GetCharaFieldValueAsStringArray(index, fieldName, out var strArray);

                // If the original array is null or shorter than desired, pad it out.
                int lineIndex = 0;
                foreach (var sub in DialogueInitialization[fieldName])
                {
                    var tokens = sub.Split(Delimiter);
                    var modDialogueName = tokens[0];
                    // Check if we have a replacement for this dialogue
                    if (charaText.SingleLines.TryGetValue(modDialogueName, out var singleLine))
                    {
                        var newLine = $"{singleLine.Line}|{singleLine.Sprite}";
                        toAdd.Add(newLine);
                    }
                    else
                    {
                        // No custom line- use the original
                        if (strArray == null || lineIndex >= strArray.Count)
                        {
                            toAdd.Add("...");
                        }
                        else if (lineIndex < strArray.Count) 
                        {
                            toAdd.Add(strArray[lineIndex]);
                        }
                    }
                    lineIndex++;
                }
                SetField(charaStructBoxed, fieldName, toAdd);
            }

            // Recreate personal 2D array
            var personalArray = new List<List<string>>(PersonalArrayLength);
            var personal0 = new List<string>(PersonalLines0.Count);
            GetCharaFieldAs2dStringArray(index, PersonalFieldName, out var originalPersonalArray);
            var personal0Index = 0;
            foreach (var personal in PersonalLines0)
            {
                var tokens = personal.Split(Delimiter);
                var modDialogueName = tokens[0];
                // Check if we have a replacement for this dialogue
                if (charaText.SingleLines.TryGetValue(modDialogueName, out var singleLine))
                {
                    var newLine = $"{singleLine.Line}|{singleLine.Sprite}";
                    personal0.Add(newLine);
                }
                else
                {
                    if (originalPersonalArray[0] == null || personal0Index >= originalPersonalArray[0].Count)
                    {
                        personal0.Add("...");
                    }
                    else if (personal0Index < originalPersonalArray.Count)
                    {
                        personal0.Add(originalPersonalArray[0][personal0Index]);
                    }
                }
                personal0Index++;
            }
            personalArray.Add(personal0);

            var personalIndex = 1;
            foreach (var dialogueName in PersonalLines1AndUp)
            {
                var newList = new List<string>();
                var tokens = dialogueName.Split(Delimiter);
                var modDialogueName = tokens[0];

                if (charaText.MultiLines.TryGetValue(modDialogueName, out var multiLine))
                {
                    // If we have a custom multiline, use it
                    foreach (var line in multiLine.Lines)
                    {
                        newList.Add($"{line.Line}|{line.Sprite}");
                    }
                }
                else if (personalIndex < originalPersonalArray.Count)
                {
                    // Otherwise, use the original dialogue if available
                    newList = originalPersonalArray[personalIndex];
                }
                else
                {
                    // If no custom or original line, use placeholder
                    newList.Add("...");
                }
                personalArray.Add(newList);
                personalIndex++;
            }

            SetField(charaStructBoxed, PersonalFieldName, personalArray);

            array.SetValue(charaStructBoxed, index);
        }
    }
}
