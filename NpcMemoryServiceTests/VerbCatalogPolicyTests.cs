// Code written by Gabriel Mailhot, 12/09/2026.
// What these pin: that the GAME ACTIONS catalogue lists what this prompt actually TAUGHT, and that narrowing
// it never silently took a capability away.
//
// THE MEASUREMENT. A Compact prompt is 15,957 characters of a 16,000-character ceiling; the catalogue is
// 7,019 of them, sixty-nine verbs at a flat 105-character median with nothing left to shorten, and the
// character himself gets 560. Worse, that was measured on a subject for whom NOT ONE availability flag was
// set - so not one gated teaching rendered, and the prompt still paid for sixty-nine verbs the game would
// have refused every one of.
//
// THE RISK THIS FILE EXISTS FOR is the opposite mistake, and it is the more expensive one: dropping a verb
// the model could otherwise have emitted. Absence does not read as "forbidden", it reads as "not mentioned",
// and the model supplies the permission itself - the 09/09 gating ruling and the 12/09 captive cut-off are
// that same lesson twice. So the rule drops a verb only on POSITIVE evidence, and the test below is what
// keeps that evidence true: it renders a maximally permissive prompt and demands that every verb named in
// Gated is genuinely taught in it. A teaching that moves or changes shape turns this red rather than turning
// a capability off.
//
// If these fail, either the catalogue is offering deeds the game will refuse again, or - far worse - a verb
// on the Gated list no longer has the teaching that justified putting it there.

#region

using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class VerbCatalogPolicyTests
   {
      private static List<GameActionDefinition> Vocabulary()
         => GameActionCatalog.All
                             .Select(s => new GameActionDefinition {Type = s.Type, Description = s.Description})
                             .ToList();

      /// <summary>
      ///   Everything a host can say yes to at once. Deliberately absurd as a character - nobody is at once a
      ///   governor-in-waiting, an escort, a consort and somebody else's prisoner - because the question it
      ///   answers is not "is this a plausible person" but "does every gated teaching still exist".
      /// </summary>
      private static EncounterContext Permissive(LeanPromptLevel lean = LeanPromptLevel.Full)
         => new() {
            LeanLevel = lean,
            PlayerStatus = PlayerStatusVsNpc.ClanMember,

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
            BastardMotherTone = "cold",
            BastardBlackmailDemand = 500
         };

      private static string Build(EncounterContext context)
         => new PromptBuilder {ActionVocabulary = Vocabulary()}
            .BuildSystemPrompt(new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"},
                               new WorldState {CurrentDay = 10},
                               context);

      // ── the guarantee that must never weaken ──

      // THE LOAD-BEARING TEST. Every name on the Gated list claims "this verb has a teaching of its own that
      // renders when the deed is possible". Here every one of those conditions is true at once, so every one
      // of those teachings must render. A name that fails this has no business being droppable.
      [Test]
      public void GIVEN_a_subject_who_may_do_everything_WHEN_built_THEN_every_gated_verb_is_genuinely_taught()
      {
         HashSet<string> taught = VerbCatalogPolicy.TaughtIn(Build(Permissive()));

         foreach (string verb in VerbCatalogPolicy.Gated)
            taught.Should().Contain(verb, $"{verb} is on the Gated list, so a teaching of its own must exist");
      }

      // THE TRAP THIS CLOSES, and it is the one that would have hurt. Some teaching sections are dropped in
      // Compact to save room - ExtraActionTeachings, the whole extended verb set, is not rendered in Lean at
      // all. For a verb taught ONLY there, the catalogue line is the only teaching a small model ever gets,
      // so gating it would not trim a duplicate, it would delete the capability outright, and only for the
      // models least able to cope. A verb may be Gated only if its own teaching survives Compact too.
      [Test]
      public void GIVEN_the_same_subject_in_compact_WHEN_built_THEN_every_gated_verb_is_STILL_taught()
      {
         HashSet<string> taught = VerbCatalogPolicy.TaughtIn(Build(Permissive(LeanPromptLevel.Lean)));

         foreach (string verb in VerbCatalogPolicy.Gated)
            taught.Should()
                  .Contain(verb,
                           $"{verb} is Gated, so Compact must still teach it - otherwise gating DELETES it");
      }

      // And when they are taught, they are listed - the catalogue is an index of the prompt, not a filter on
      // it. Limited to verbs the catalogue actually holds: rescue_prisoner is taught by the prompt and is not
      // a GameActionCatalog entry at all (it reaches the game by another channel), so gating it can neither
      // help nor harm, and asserting it here would only pin an unrelated fact.
      [Test]
      public void GIVEN_a_subject_who_may_do_everything_WHEN_built_THEN_every_gated_verb_it_holds_is_also_offered()
      {
         string prompt = Build(Permissive());
         var inCatalogue = new HashSet<string>(Vocabulary().Select(v => v.Type));

         foreach (string verb in VerbCatalogPolicy.Gated.Where(inCatalogue.Contains))
            prompt.Should().Contain("- " + verb + ":");
      }

      // The saving, stated as a number so it cannot quietly evaporate. Same vocabulary, same builder; the only
      // difference is whether the deeds are possible.
      [Test]
      public void GIVEN_a_subject_who_may_do_none_of_them_WHEN_built_THEN_the_prompt_is_thousands_of_chars_shorter()
      {
         int everything = Build(Permissive()).Length;
         int nothing = Build(new EncounterContext {LeanLevel = LeanPromptLevel.Full}).Length;

         (everything - nothing).Should().BeGreaterThan(2000);
      }

      // ── the rule itself ──

      // Unknown is not closed. A verb with no gated teaching of its own is listed whatever the prompt says,
      // because "we could not find a teaching" must never be read as "the game refuses it".
      [Test]
      public void GIVEN_a_verb_with_no_gate_WHEN_the_prompt_never_mentions_it_THEN_it_is_still_offered()
      {
         var nothingTaught = new HashSet<string>();

         VerbCatalogPolicy.Offered("change_relation", nothingTaught).Should().BeTrue();
         VerbCatalogPolicy.Offered("give_gold", nothingTaught).Should().BeTrue();
         VerbCatalogPolicy.Offered("appoint_governor", nothingTaught).Should().BeFalse();
      }

      // Read back out of the finished text rather than out of a map of the code, which is what makes it
      // incapable of drifting from it - and what lets a teaching arriving from the mod's own verb registry
      // through ExtraActionTeachings be seen exactly like one written here.
      [Test]
      public void GIVEN_a_prompt_WHEN_scanned_THEN_it_finds_the_action_blocks_and_nothing_that_merely_looks_like_one()
      {
         HashSet<string> found = VerbCatalogPolicy.TaughtIn(
            "[ACTION]\ntype: appoint_governor\n[/ACTION]\nthe word type: is not a block here, nor Type: Grand\n");

         found.Should().Contain("appoint_governor");
         found.Should().HaveCount(1);
      }

      // Refusing rather than throwing, the shape every entry point here has - and a second call must be a
      // no-op, so a prompt that has already been resolved is never mangled by resolving it twice.
      [Test]
      public void GIVEN_no_placeholder_WHEN_resolved_THEN_the_prompt_comes_back_exactly_as_it_was()
      {
         const string plain = "a prompt with no catalogue in it";

         VerbCatalogPolicy.Resolve(plain, Vocabulary(), LeanPromptLevel.Lean).Should().Be(plain);
         VerbCatalogPolicy.Resolve(null!, Vocabulary(), LeanPromptLevel.Lean).Should().BeNull();
         VerbCatalogPolicy.Resolve(VerbCatalogPolicy.Placeholder, null, LeanPromptLevel.Lean)
                          .Should()
                          .BeEmpty();
      }

      // The placeholder must never survive into a prompt a model reads: it is the one failure of this design
      // that would be visible to a player rather than merely expensive.
      [Test]
      public void GIVEN_any_prompt_WHEN_built_THEN_the_placeholder_never_reaches_the_model()
      {
         Build(Permissive()).Should().NotContain(VerbCatalogPolicy.Placeholder);
         Build(new EncounterContext {LeanLevel = LeanPromptLevel.Lean})
            .Should()
            .NotContain(VerbCatalogPolicy.Placeholder);
      }
   }
}
