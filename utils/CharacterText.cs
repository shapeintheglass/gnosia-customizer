using System.Collections.Generic;

namespace GnosiaCustomizer.utils
{
    public class CharacterText
    {

        [InternalName("name")]
        public LocalizedText Name { get; set; } = default;
        [InternalName("d_place")]
        public LocalizedText Origin { get; set; } = default;
        [InternalName("t_temp___0")]
        public LocalizedText CrewData0 { get; set; } = default;
        [InternalName("t_temp___1")]
        public LocalizedText CrewData1 { get; set; } = default;
        [InternalName("t_keisho")]
        public LocalizedText Honorific { get; set; } = default;
        [InternalName("sex")]
        public byte? Sex { get; set; } = 2; // 0 = Male, 1 = Female, 2 = Non-Binary
        [InternalName("age")]
        public uint? Age { get; set; } = 25;
        public List<string> Tags { get; set; } = default;
        [InternalName("d_tokkiNum")]
        public byte? NumJournalEntries { get; set; } = 6;
        [InternalName("d_tokki")]
        public MultilineLocalizedText MultilineJournalEntries { get; set; } = default;
        [InternalName("attr")]
        public Dictionary<string, float> Attributes { get; set; } = default;
        [InternalName("abil")]
        public Dictionary<string, float> AbilityStart { get; set; } = default;
        [InternalName("abilMax")]
        public Dictionary<string, float> AbilityMax { get; set; } = default;
        public Dictionary<string, bool> KnownSkills { get; set; } = default;
        [InternalName("likePlace")]
        public Dictionary<string, bool> PreferredPlaces { get; set; } = default;
        [InternalName("hatePlace")]
        public Dictionary<string, bool> DislikedPlaces { get; set; } = default;
        [InternalName("hpMin")]
        public int? DefenseMin { get; set; } = 100;
        [InternalName("hpWithGnos")]
        public int? DefenseWithGnos { get; set; } = 150;
        [InternalName("t_personal___0___16")]
        public LocalizedText OpeningStatement { get; set; } = default;
        [InternalName("t_okuyami___0")]
        public LocalizedText OpeningRemarksCondolences { get; set; } = default;
        [InternalName("t_okuyami_n___0")]
        public LocalizedText OpeningRemarksNoDeaths { get; set; } = default;
        [InternalName("t_co_req___0")]
        public LocalizedText StepForward { get; set; } = default;
        [InternalName("t_co___1")]
        public LocalizedText RevealRoleEngineer { get; set; } = default;
        [InternalName("t_co_taiko___1")]
        public LocalizedText RevealOwnRoleEngineer { get; set; } = default;
        [InternalName("t_co_find___1")]
        public LocalizedText RevealRoleEngineerWithGnosiaResult { get; set; } = default;
        [InternalName("t_co___2")]
        public LocalizedText RevealRoleDoctor { get; set; } = default;
        [InternalName("t_co_taiko___2")]
        public LocalizedText RevealOwnRoleDoctor { get; set; } = default;
        [InternalName("t_co_find___2")]
        public LocalizedText RevealRoleDoctorWithGnosiaResult { get; set; } = default;
        [InternalName("t_co___4")]
        public LocalizedText RevealRoleGuardDuty { get; set; } = default;
        [InternalName("t_co_taiko___4")]
        public LocalizedText RevealRoleGuardDutyResponse { get; set; } = default;
        [InternalName("t_co___3")]
        public LocalizedText RevealRoleGuardianAngel { get; set; } = default;
        [InternalName("t_co_after___0")]
        public LocalizedText StepForward1ClaimReaction { get; set; } = default;
        [InternalName("t_co_after___1")]
        public LocalizedText StepForward2ClaimsReaction { get; set; } = default;
        [InternalName("t_co_after___2")]
        public LocalizedText StepForward3ClaimsReaction { get; set; } = default;
        [InternalName("t_houkoku_req___0")]
        public LocalizedText RequestReport { get; set; } = default;
        [InternalName("t_houkoku_not___0")]
        public LocalizedText RequestReportNoResponse { get; set; } = default;
        [InternalName("t_houkoku_s___0")]
        public LocalizedText ReportPt1MatchingResult { get; set; } = default;
        [InternalName("t_houkoku_w___0")]
        public LocalizedText ReportPt1ConflictingResult { get; set; } = default;
        [InternalName("t_uranai_o___0")]
        public LocalizedText ReportEngineerPt1TargetKilled { get; set; } = default;
        [InternalName("t_uranai_s___0")]
        public LocalizedText ReportEngineerPt1 { get; set; } = default;
        [InternalName("t_uranai_b___0")]
        public LocalizedText ReportEngineerPt1DifferentTarget { get; set; } = default;
        [InternalName("t_uranai_t___0")]
        public LocalizedText ReportEngineerPt2GnosiaResult { get; set; } = default;
        [InternalName("t_uranai_f___0")]
        public LocalizedText ReportEngineerPt2HumanResult { get; set; } = default;
        [InternalName("t_uranai_n___0")]
        public LocalizedText ReportEngineerNoResult { get; set; } = default;
        [InternalName("t_reibai_s___0")]
        public LocalizedText ReportDoctorPt1 { get; set; } = default;
        [InternalName("t_reibai_t___0")]
        public LocalizedText ReportDoctorPt2SingleGnosiaResult { get; set; } = default;
        [InternalName("t_reibai_f___0")]
        public LocalizedText ReportDoctorPt2SingleHumanResult { get; set; } = default;
        [InternalName("t_reibai_ft___0")]
        public LocalizedText ReportDoctorPt2MultipleGnosiaResult { get; set; } = default;
        [InternalName("t_reibai_ff___0")]
        public LocalizedText ReportDoctorPt2OopsAllHumansResult { get; set; } = default;
        [InternalName("t_personal___0___11")]
        public LocalizedText TimeClam { get; set; } = default;
        [InternalName("t_suspect_add___0")]
        public LocalizedText DoubtDayOne { get; set; } = default;
        [InternalName("t_suspect___0")]
        public LocalizedText DoubtDislike { get; set; } = default;
        [InternalName("t_suspect___1")]
        public LocalizedText DoubtTooChatty { get; set; } = default;
        [InternalName("t_suspect___2")]
        public LocalizedText DoubtTooPopular { get; set; } = default;
        [InternalName("t_suspect___3")]
        public LocalizedText DoubtTooQuiet { get; set; } = default;
        [InternalName("t_suspect___4")]
        public LocalizedText DoubtProb { get; set; } = default;
        [InternalName("t_suspect___5")]
        public LocalizedText DoubtTrusted { get; set; } = default;
        [InternalName("t_suspect___6")]
        public LocalizedText DoubtCollaborator { get; set; } = default;
        [InternalName("t_suspect___7")]
        public LocalizedText DoubtAvenge { get; set; } = default;
        [InternalName("t_suspect_r___0")]
        public LocalizedText DoubtTrustVariantDislike { get; set; } = default;
        [InternalName("t_suspect_r___1")]
        public LocalizedText DoubtTrustVariantTooChatty { get; set; } = default;
        [InternalName("t_suspect_r___2")]
        public LocalizedText DoubtTrustVariantTooPopular { get; set; } = default;
        [InternalName("t_suspect_r___3")]
        public LocalizedText DoubtTrustVariantTooQuiet { get; set; } = default;
        [InternalName("t_suspect_r___4")]
        public LocalizedText DoubtTrustVariantProb { get; set; } = default;
        [InternalName("t_suspect_r___5")]
        public LocalizedText DoubtTrustVariantTrusted { get; set; } = default;
        [InternalName("t_suspect_r___6")]
        public LocalizedText DoubtTrustVariantCollaborator { get; set; } = default;
        [InternalName("t_suspect_r___7")]
        public LocalizedText DoubtTrustVariantAvenge { get; set; } = default;
        [InternalName("t_suspect_t0___0")]
        public LocalizedText DoubtAffirmDislike { get; set; } = default;
        [InternalName("t_suspect_t0___1")]
        public LocalizedText DoubtAffirmTooChatty { get; set; } = default;
        [InternalName("t_suspect_t0___2")]
        public LocalizedText DoubtAffirmTooPopular { get; set; } = default;
        [InternalName("t_suspect_t0___3")]
        public LocalizedText DoubtAffirmTooQuiet { get; set; } = default;
        [InternalName("t_suspect_t0___4")]
        public LocalizedText DoubtAffirmProb { get; set; } = default;
        [InternalName("t_suspect_t0___5")]
        public LocalizedText DoubtAffirmTrusted { get; set; } = default;
        [InternalName("t_suspect_t0___6")]
        public LocalizedText DoubtAffirmNegativeTrust { get; set; } = default;
        [InternalName("t_suspect_t1___0")]
        public LocalizedText DoubtAffirmTrustVariantDislike { get; set; } = default;
        [InternalName("t_suspect_t1___1")]
        public LocalizedText DoubtAffirmTrustVariantTooChatty { get; set; } = default;
        [InternalName("t_suspect_t1___2")]
        public LocalizedText DoubtAffirmTrustVariantTooPopular { get; set; } = default;
        [InternalName("t_suspect_t1___3")]
        public LocalizedText DoubtAffirmTrustVariantTooQuiet { get; set; } = default;
        [InternalName("t_suspect_t1___4")]
        public LocalizedText DoubtAffirmTrustVariantProb { get; set; } = default;
        [InternalName("t_suspect_t1___5")]
        public LocalizedText DoubtAffirmTrustVariantTrusted { get; set; } = default;
        [InternalName("t_suspected0___0")]
        public LocalizedText RefuteWeakDoubt { get; set; } = default;
        [InternalName("t_suspected0___1")]
        public LocalizedText RefuteStrongDoubt { get; set; } = default;
        [InternalName("t_hanron0___0")]
        public LocalizedText DefendGeneric { get; set; } = default;
        [InternalName("t_hanron0___1")]
        public LocalizedText DefendTrustedTarget { get; set; } = default;
        [InternalName("t_hanron1___0")]
        public LocalizedText DefendUntrustedTarget { get; set; } = default;
        [InternalName("t_hanron_t0___0")]
        public LocalizedText JoinDefenseTrustedTarget { get; set; } = default;
        [InternalName("t_hanron_t1___0")]
        public LocalizedText JoinDefenseUntrustedTarget { get; set; } = default;
        [InternalName("t_trust___0")]
        public LocalizedText CoverVouch { get; set; } = default;
        [InternalName("t_trust___1")]
        public LocalizedText CoverProb { get; set; } = default;
        [InternalName("t_trust___2")]
        public LocalizedText CoverTrusted { get; set; } = default;
        [InternalName("t_trust_r___0")]
        public LocalizedText CoverLowTrustVariantVouch { get; set; } = default;
        [InternalName("t_trust_r___1")]
        public LocalizedText CoverLowTrustVariantProb { get; set; } = default;
        [InternalName("t_trust_r___2")]
        public LocalizedText CoverLowTrustVariantTrusted { get; set; } = default;
        [InternalName("t_trust_t0___0")]
        public LocalizedText CoverTogether { get; set; } = default;
        [InternalName("t_trust_t1___0")]
        public LocalizedText CoverTogetherLowTrustVariant { get; set; } = default;
        [InternalName("t_trusted0___0")]
        public LocalizedText SayThanks { get; set; } = default;
        [InternalName("t_thanron0___0")]
        public LocalizedText Argue { get; set; } = default;
        [InternalName("t_thanron1___0")]
        public LocalizedText ArgueHighTrustVariant { get; set; } = default;
        [InternalName("t_thanron_t0___0")]
        public LocalizedText JoinArgument { get; set; } = default;
        [InternalName("t_thanron_t1___0")]
        public LocalizedText JoinArgumentHighTrustVariant { get; set; } = default;
        [InternalName("t_hosho___0")]
        public LocalizedText DefiniteHumanWithRole { get; set; } = default;
        [InternalName("t_hosho___1")]
        public LocalizedText DefiniteHuman { get; set; } = default;
        [InternalName("t_hosho_enemy___0")]
        public LocalizedText DefiniteEnemyWithRole { get; set; } = default;
        [InternalName("t_hosho_enemy___1")]
        public LocalizedText DefiniteEnemy { get; set; } = default;
        [InternalName("t_hosho_enemy___2")]
        public LocalizedText DefiniteEnemyAc { get; set; } = default;
        [InternalName("t_hosho_enemy___3")]
        public LocalizedText DefiniteEnemyBug { get; set; } = default;
        [InternalName("t_hosho_enemy___4")]
        public LocalizedText DefiniteEnemyLiar { get; set; } = default;
        [InternalName("t_hosho_enemy___5")]
        public LocalizedText DefiniteEnemyLiarNotGnosia { get; set; } = default;
        [InternalName("t_hosho_miss___0")]
        public LocalizedText PointOutMistake { get; set; } = default;
        [InternalName("t_hosho_miss___1")]
        public LocalizedText ThatsObvious { get; set; } = default;
        [InternalName("t_hosho_get___0")]
        public LocalizedText SayThanksDefiniteHumanWithRole { get; set; } = default;
        [InternalName("t_hosho_get___1")]
        public LocalizedText DefiniteEnemyRevealed { get; set; } = default;
        [InternalName("t_tohyo_go___0")]
        public LocalizedText LetsVoteNothingToSay { get; set; } = default;
        [InternalName("t_tohyo_mae___0")]
        public LocalizedText LetsVote { get; set; } = default;
        [InternalName("t_tohyo_sai___0")]
        public LocalizedText LetsVoteTiebreaker { get; set; } = default;
        [InternalName("t_shokei___0")]
        public LocalizedText TiebreakerVoteIndecisive { get; set; } = default;
        [InternalName("t_shokei___1")]
        public LocalizedText VoteSkipped { get; set; } = default;
        public LocalizedText FreezeEveryoneProposal { get; set; } = default;
        public LocalizedText FreezeNobodyProposal { get; set; } = default;
        public LocalizedText FreezeEveryoneAgree { get; set; } = default;
        public LocalizedText FreezeEveryoneDisagree { get; set; } = default;
        public LocalizedText FreezeNobodyAgree { get; set; } = default;
        public LocalizedText FreezeNobodyDisagree { get; set; } = default;
        public LocalizedText FreezeEveryoneAccepted { get; set; } = default;
        public LocalizedText FreezeNobodyAccepted { get; set; } = default;
        public LocalizedText FreezeProposalRejected { get; set; } = default;
        public LocalizedText ColdSleep { get; set; } = default;
        public LocalizedText ColdSleepAsDefiniteEnemy { get; set; } = default;
        public LocalizedText FarewellToColdSleeper { get; set; } = default;
        public LocalizedText SkIntuiSayHumanProposal { get; set; } = default;
        public LocalizedText SkIntuiSayHumanYesImHuman { get; set; } = default;
        public LocalizedText SkIntuiSayHumanStopIt { get; set; } = default;
        public LocalizedText SkIntuiSayHumanNoOneResponded { get; set; } = default;
        public LocalizedText SkIntuiSayHumanSomeResponded { get; set; } = default;
        public LocalizedText SkIntuiSayHumanAllResponded { get; set; } = default;
        public LocalizedText SkIntuiSayHumanStoppedReaction { get; set; } = default;
        public LocalizedText SkStealSmallTalkStartFood { get; set; } = default;
        public LocalizedText SkStealSmallTalkStartLove { get; set; } = default;
        public LocalizedText SkStealSmallTalkStartScary { get; set; } = default;
        public LocalizedText SkStealSmallTalkJoinFood { get; set; } = default;
        public LocalizedText SkStealSmallTalkJoinLove { get; set; } = default;
        public LocalizedText SkStealSmallTalkJoinScary { get; set; } = default;
        public LocalizedText SkStealSmallTalkStop { get; set; } = default;
        public LocalizedText SkLogicFreezeAllInitialProposal { get; set; } = default;
        public LocalizedText SkLogicFreezeAllInitialProposalSomeMissing { get; set; } = default;
        public LocalizedText SkLogicFreezeAllAgree { get; set; } = default;
        public LocalizedText SkLogicFreezeAllDisagree { get; set; } = default;
        public LocalizedText SkLogicFreezeAllDisagreeFollowup { get; set; } = default;
        public LocalizedText SkLogicFreezeAllProposalAccepted { get; set; } = default;
        public LocalizedText SkLogicFreezeAllProposalDenied { get; set; } = default;
        public LocalizedText SkLogicVoteProposalFromEngReport { get; set; } = default;
        public LocalizedText SkLogicVoteProposalWithSelfBasis { get; set; } = default;
        public LocalizedText SkLogicVoteProposalForDefiniteEnemy { get; set; } = default;
        public LocalizedText SkLogicVotePointOutMistake { get; set; } = default;
        public LocalizedText SkLogicVoteAgree { get; set; } = default;
        public LocalizedText SkLogicVoteDefendSelf { get; set; } = default;
        public LocalizedText SkLogicVoteDisagree { get; set; } = default;
        public LocalizedText SkLogicVoteDisagreeAlso { get; set; } = default;
        public LocalizedText SkLogicDontVoteProposal { get; set; } = default;
        public LocalizedText SkLogicDontVoteAgree { get; set; } = default;
        public LocalizedText SkLogicDontVoteDisagree { get; set; } = default;
        public LocalizedText SkLogicDontVoteDisagreeAlso { get; set; } = default;
        public LocalizedText SkCharmCollabProposal { get; set; } = default;
        public LocalizedText SkCharmCollabAccept { get; set; } = default;
        public LocalizedText SkCharmCollabDecline { get; set; } = default;
        public LocalizedText SkCharmSeekAgreement { get; set; } = default;
        public LocalizedText SkChariBlockArgument { get; set; } = default;
        public LocalizedText SkPerfoExaggerateDoubt { get; set; } = default;
        public LocalizedText SkPerfoExaggerateCover { get; set; } = default;
        public LocalizedText SkPerfoExaggerateSupportCounter { get; set; } = default;
        public LocalizedText SkPerfoExaggerateDontVote { get; set; } = default;
        public LocalizedText SkStealObfuscate { get; set; } = default;
        public LocalizedText SkPerfoRetaliate { get; set; } = default;
        public LocalizedText SkCharmRegret { get; set; } = default;
        public LocalizedText SkPerfoSeekHelp { get; set; } = default;
        public LocalizedText SkPerfoSeekHelpReaction { get; set; } = default;
        public LocalizedText SkIntuiDontBeFooled { get; set; } = default;
        public LocalizedText SkStealthGrovel { get; set; } = default;
        public LocalizedText SkStealthGrovelReaction { get; set; } = default;
        public LocalizedText NightFriendAndHighTrust { get; set; } = default;
        public LocalizedText NightFriendAndMaybeTrusted { get; set; } = default;
        public LocalizedText NightMaybeFriendAndMaybeTrusted { get; set; } = default;
        public LocalizedText NightMaybeFriendAndNotTrusted { get; set; } = default;
        public LocalizedText NightNotFriendAndMaybeTrusted { get; set; } = default;
        public LocalizedText NightReallyNotFriendAndMaybeTrusted { get; set; } = default;
        public LocalizedText NightDefinitelyNotFriend { get; set; } = default;
        public LocalizedText NightCharDefiniteEnemy { get; set; } = default;
        public LocalizedText NightPlayerDefiniteEnemy { get; set; } = default;
        public LocalizedText NightPlayerIsDefEnemyBothGnosia { get; set; } = default;
        public LocalizedText NightOpposingClaim { get; set; } = default;
        public LocalizedText NightBothDefiniteHuman { get; set; } = default;
        public LocalizedText NightPlayerDefiniteHuman { get; set; } = default;
        public LocalizedText NightCharDefHuman { get; set; } = default;
        public LocalizedText NightBothGnosia { get; set; } = default;
        public LocalizedText MultilineNightLiarFound { get; set; } = default;
        public LocalizedText MultilineLiarFoundFollowup { get; set; } = default;
        public LocalizedText MultilineNightLetsCollaborate { get; set; } = default;
        public LocalizedText MultilineNightLetsCollaborateAccepted { get; set; } = default;
        public LocalizedText MultilineNightLetsCollaborateDeclined { get; set; } = default;
        public LocalizedText MultilineNightGnosiaLetsEliminate { get; set; } = default;
        public LocalizedText MultilineGnosiaLetsEliminateFollowup { get; set; } = default;
        public LocalizedText MultilineEndHumanWinWithCollaborator { get; set; } = default;
        public LocalizedText MultilineEndHumanWin { get; set; } = default;
        public LocalizedText MultilineEndHumanWinNotTrusted { get; set; } = default;
        public LocalizedText MultilineEndHumanWinSomewhatFriends { get; set; } = default;
        public LocalizedText MultilineEndHumanWinNotFriends { get; set; } = default;
        public LocalizedText MultilineEndGnosiaWinTogether0 { get; set; } = default;
        public LocalizedText MultilineEndGnosiaPerfectWinTogether0 { get; set; } = default;
        public LocalizedText MultilineEndGnosiaWinTogether1 { get; set; } = default;
        public LocalizedText MultilineEndGnosiaPerfectWinTogether1 { get; set; } = default;
        public LocalizedText MultilineEndCharIsOpposingGnosia { get; set; } = default;
        public LocalizedText MultilineEndCharIsBug { get; set; } = default;
        public LocalizedText MultilineEndPlayerIsAc { get; set; } = default;
    }

    public class LocalizedText
    {
        public string Line { get; set; } = default;
        public int Sprite { get; set; } = 0;
        public List<OverrideData> Overrides { get; set; } = default;
    }

    public class MultilineLocalizedText
    {
        public List<LocalizedText> Lines { get; set; } = new List<LocalizedText>();
    }

    public class OverrideData
    {
        public Dictionary<string, string> Conditions { get; set; } = [];
        public string Line { get; set; } = default;
        public int Sprite { get; set; } = 0;
    }
}
