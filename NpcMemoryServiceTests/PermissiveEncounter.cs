// Code written by Gabriel Mailhot, 25/10/2026.
// Everything a host can say yes to at once, in one place. It was VerbCatalogPolicyTests' own Permissive() until the
// composition-parity map (Calradia Remembers, docs/maps/composition-parity.md, 25/10/2026) needed the very same subject:
// that map renders the prose prompt and the interpreter's from one context to compare what each is taught, and a copy of
// this list in the mod's tests would drift from the original the first time a gate was added here. The mod's test
// project compiles this file by link, so both suites read the same subject.

#region

using System.Collections.Generic;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;

#endregion

namespace NpcMemoryServiceTests
{
   /// <summary>A subject for whom every gated action teaching renders.</summary>
   public static class PermissiveEncounter
   {
      /// <summary>
      ///   Everything a host can say yes to at once. Deliberately absurd as a character - nobody is at once a
      ///   governor-in-waiting, an escort, a consort and somebody else's prisoner - because the question it
      ///   answers is not "is this a plausible person" but "does every gated teaching still exist".
      ///   <paramref name="storyChoicesForInterpreter" /> is a written tale's open choices by words, as the host composes
      ///   them; null when the caller has none.
      /// </summary>
      public static EncounterContext Build(LeanPromptLevel lean = LeanPromptLevel.Full, string? storyChoicesForInterpreter = null)
         => new() {
            LeanLevel = lean,
            // counter_offer (14/10/2026) is taught only where the captive-terms section renders, which needs
            // PlayerStatus == NpcIsCaptive; none of the other Gated teachings key off PlayerStatus == ClanMember
            // specifically (they only ever exclude PlayerStatus == Captive, a different value), so this does not
            // suppress anything else on this list.
            PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive,
            CaptiveDisposition = "offers_first",
            CaptiveTerms = new List<string> {"ransom", "fealty", "service", "intimacy"},
            CaptiveRansomCeiling = 5000,
            CaptiveFealtyRealm = "the Northern Empire",

            NpcCanBeAppointedGovernor = true, NpcCanBeAssignedPartyRole = true, NpcCanRejoinParty = true,
            NpcCanBeDispatchedOnMission = true, PlayerCanMediatePeace = true, NpcCanBeGrantedFief = true,
            NpcCanHaveFiefRevoked = true, NpcCanBeExpelledFromClan = true, NpcCanBeGrantedStipend = true,
            NpcCanSwearOath = true,

            NpcCanEscortPlayer = true, NpcIsEscortingPlayer = true, NpcCanRideWithPlayer = true,
            NpcIsRidingWithPlayer = true,

            ConsortEligible = true, SecretLoverEligible = true, OpenRelationshipEligible = true,
            PartnerLoverKnown = true, PartnerLoverName = "Liena",

            CanRecruitPrisoner = true, CanRecruitNotable = true, NpcIsPrisonerOfAnother = true,
            // take_into_service: the one verb that points OUTWARD, so the permissive subject has to be
            // someone the player could give a companion TO, not just someone who could join them.
            CompanionsOfferableToThisLord = "Nabb the Bloody Handed",
            TeachableSkills = new List<string> {"Riding", "Bow"},

            LordRecruitEligible = true, PlayerIsMercenary = true, CompanionOnErrand = true,
            SchemeTargetsThisNpc = true,

            // The second wave, each one a gate the first pass read past.
            SchemeAgentTargetName = "Rhagaea",
            CompanionMoodNote = "restless, and saying so",
            CompanionAudience = CompanionAudienceReason.Retirement,
            AudienceRetirementIsLanded = true,
            BastardMotherTone = "mercenary",
            BastardBlackmailDemand = 500,

            // The third wave (25/10/2026), found by the composition-parity map: offers it left closed, so their verbs
            // read as never taught to the prose model when they are, behind these.
            WarStatus = DiplomaticStatus.AtPeace,
            AcceptOtherPartnersEligible = true, LoveMatchEligible = true,
            MarriageProspects = "Liena of dey Meroc, unwed", MatchmakerNpcKin = "Liena", MatchmakerPlayerKin = "Eric",
            MercenaryOfferKingdom = "Vlandia", VassalOfferKingdom = "Vlandia", CompanionAskingPrice = 800,
            Witnesses = new List<WitnessEntry> {new() {Name = "Ira", RelationToNpc = "a stranger", IsPlayerCompanion = true}},

            StoryChoicesForInterpreter = storyChoicesForInterpreter
         };

      /// <summary>
      ///   Every gate cannot be open in one context: the player held captive and the player holding a captive are two
      ///   branches of the prompt, and the free player's offers close while the player is a prisoner. So the subjects
      ///   that together open every action teaching, each named. Render them with the builder's adult level at
      ///   Hardcore (it is a property of the builder, not of the context).
      /// </summary>
      public static IReadOnlyList<(string Name, EncounterContext Context)> Scenarios(LeanPromptLevel lean = LeanPromptLevel.Full,
                                                                                    string? storyChoicesForInterpreter = null)
         => new List<(string, EncounterContext)> {
            ("everything open", Build(lean, storyChoicesForInterpreter)),
            ("the player held captive", new EncounterContext {
               LeanLevel = lean, PlayerStatus = PlayerStatusVsNpc.Captive, CaptiveIntent = CaptiveSceneIntent.Domination,
               CaptorMayExecutePlayer = true, StoryChoicesForInterpreter = storyChoicesForInterpreter
            }),
            ("the player's captor scene", new EncounterContext {
               LeanLevel = lean, PlayerStatus = PlayerStatusVsNpc.NpcIsCaptive, IsCaptorScene = true,
               StoryChoicesForInterpreter = storyChoicesForInterpreter
            }),
            // A held secret is leverage only with a hostile realm or low regard (HeldSecretPolicy), and the free player's
            // context is at peace for the prisoner gift, so this one is its own.
            ("a hostile lord who holds the player's secret", new EncounterContext {
               LeanLevel = lean, WarStatus = DiplomaticStatus.AtWar, HeldSecretLoverSubject = "Liena",
               StoryChoicesForInterpreter = storyChoicesForInterpreter
            })
         };
   }
}
