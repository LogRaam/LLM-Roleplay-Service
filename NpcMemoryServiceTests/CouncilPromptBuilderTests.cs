// Code written by Gabriel Mailhot, 22/08/2026.
// The council's new ONE-CALL GROUP SCENE prompt, replacing the old anchor-plus-[WITNESS_REACTION] shape (one
// seated member spoke the real [DIALOGUE], the rest merely reacted and often stayed silent). These tests pin
// the contract CouncilResponseParser (and later, the mod's own handler) depends on: every roster member is
// named, the [SPEAKER: Name] format is taught, every member is required to speak, the [RESOLUTION] format stays
// byte-for-byte what the existing council catalogue already parses, actor-attributed change_relation is taught,
// and no deed/[QUEST]/[EVENT] leaks into this table.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class CouncilPromptBuilderTests
   {
      private static CouncilPromptInput MinimalInput(params string[] names)
      {
         var roster = new List<CouncilMemberInput>();
         foreach (string name in names)
            roster.Add(new CouncilMemberInput {Name = name, RegardTowardPlayer = 0});

         return new CouncilPromptInput {Roster = roster};
      }

      // Baseline: without every roster member's name actually appearing, the model has nothing to echo back in
      // its own [SPEAKER: Name] tags, and CouncilResponseParser's tolerant match has no seat to resolve against.
      [Test]
      public void GIVEN_a_roster_of_members_WHEN_building_the_prompt_THEN_every_member_is_named()
      {
         var input = MinimalInput("Ajin the Hawk", "Hophtalamos the Shipwright");

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("Ajin the Hawk");
         prompt.Should().Contain("Hophtalamos the Shipwright");
      }

      // The contract's own output shape: without this the model has no idea a per-speaker attributed block is
      // expected at all, and would fall back to a single undifferentiated reply.
      [Test]
      public void GIVEN_any_input_WHEN_building_the_prompt_THEN_the_speaker_tag_format_is_taught()
      {
         string prompt = CouncilPromptBuilder.Build(MinimalInput("Ajin"));

         prompt.Should().Contain("[SPEAKER: Name]");
         prompt.Should().Contain("[SCENE]");
      }

      // The author's own ask: brief narrator "camera" hand-offs interleaved BETWEEN speakers (a hand-off of the
      // floor, a reaction), not just a single leading beat. Without this the model would still believe [SCENE]
      // is a one-shot opener and never place a transition mid-scene.
      [Test]
      public void GIVEN_any_input_WHEN_building_the_prompt_THEN_interleaved_scene_transitions_between_speakers_are_taught()
      {
         string prompt = CouncilPromptBuilder.Build(MinimalInput("Ajin"));

         prompt.Should().Contain("between any two [SPEAKER] blocks");
         prompt.Should().NotContain("[SCENE] optional, at most once");
      }

      // The opening turn (2026-08-22, Council Increment 4): the council now OPENS through this same group-scene
      // call, not the old chat narrator path, so the sitting's one-time scene-setting (a war council, a wary
      // parley envoy) has to reach the model as a stage direction, never as words the player supposedly spoke.
      // Without this an opener would either lose its scope flavour or be voiced back as if the player had said it.
      [Test]
      public void GIVEN_an_opening_cue_WHEN_building_the_prompt_THEN_it_is_set_as_a_situation_not_a_player_line()
      {
         var input = new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {new() {Name = "Ajin", RegardTowardPlayer = 0}},
            PlayerName = "Rhagaea",
            OpeningCue = "The player has sought a PARLEY: the enemy's envoy sits across the table, wary."
         };

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("THE SITTING IS JUST OPENING. THE SITUATION:");
         prompt.Should().Contain("sought a PARLEY");
         prompt.Should().Contain("the table opens on its own"); // no player line this turn
         prompt.Should().NotContain("Rhagaea: The player has sought a PARLEY"); // never voiced as the player's own words
      }

      // Player report 2026-08-23: the model wrote "he turns his gaze toward the player" instead of using the
      // character's name. The stable head necessarily says "the player" as instruction vocabulary, so the prompt
      // must both NAME the player and forbid that system label in the output, or the model echoes it.
      [Test]
      public void GIVEN_a_player_name_WHEN_building_the_prompt_THEN_the_model_is_told_to_name_the_player_not_say_the_player()
      {
         var input = new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {new() {Name = "Ajin", RegardTowardPlayer = 0}},
            PlayerName = "Arwa"
         };

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("THE PLAYER AT THIS TABLE IS NAMED Arwa");
         prompt.Should().Contain("NEVER write \"the player\" in your output");
      }

      // Player report 2026-08-23: family-council members should act their age (a child as a child). The seat's
      // age must reach the model, and the rule that binds a member's voice to their life stage must be taught.
      [Test]
      public void GIVEN_a_member_with_an_age_WHEN_building_the_prompt_THEN_the_age_and_the_speak_true_to_age_rule_are_shown()
      {
         var input = new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {new() {Name = "Alya", RegardTowardPlayer = 0, Age = 9}}
         };

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("aged 9");
         prompt.Should().Contain("true to their AGE");
      }

      // The whole point of the rebuild Gabriel asked for: one councillor must never again dominate a sitting
      // while the rest fade silently.
      [Test]
      public void GIVEN_any_input_WHEN_building_the_prompt_THEN_every_seated_member_must_speak_is_enforced()
      {
         string prompt = CouncilPromptBuilder.Build(MinimalInput("Ajin"));

         prompt.Should().Contain("EVERY SEATED MEMBER LISTED BELOW MUST SPEAK, AT LEAST ONCE, THIS TURN:");
      }

      // The [RESOLUTION] block's field names (type/actor/target_settlement/detail) MUST be byte-for-byte the
      // existing format, or the mod's existing FinalizeCouncilResolutions/SealCouncilEngagements/CouncilLift
      // (which already parse this exact shape from the older council path) would silently stop matching.
      [Test]
      public void GIVEN_any_input_WHEN_building_the_prompt_THEN_the_exact_resolution_format_is_taught()
      {
         string prompt = CouncilPromptBuilder.Build(MinimalInput("Ajin"));

         prompt.Should().Contain("[RESOLUTION]");
         prompt.Should().Contain("type: quest");
         prompt.Should().Contain("actor: <the member's name exactly as listed below>");
         prompt.Should().Contain("target_settlement: <the town or village the deed concerns, spelled exactly as a real place>");
         prompt.Should().Contain("detail: <what they pledge, in plain words>");
         prompt.Should().Contain("[/RESOLUTION]");
      }

      // The regard-is-real channel: without this the model has no positive way to register a genuine, immediate
      // shift in one member's feeling, and (per the ratified 2026-07-24 design) would either invent its own tag
      // or wrongly treat every reaction as a deferred [RESOLUTION].
      [Test]
      public void GIVEN_any_input_WHEN_building_the_prompt_THEN_actor_attributed_change_relation_is_taught()
      {
         string prompt = CouncilPromptBuilder.Build(MinimalInput("Ajin"));

         prompt.Should().Contain("type: change_relation");
         prompt.Should().Contain("actor: <the member whose regard moved, exactly as listed below>");
         prompt.Should().Contain("delta:");
      }

      // The council's own "no deed is sealed" invariant: a gold/troop/marriage/scheme deed, or a bare [QUEST]/
      // [QUEST_COMPLETE]/[EVENT] block, must never be carried out at the table itself.
      [Test]
      public void GIVEN_any_input_WHEN_building_the_prompt_THEN_deeds_and_quest_and_event_blocks_are_forbidden()
      {
         string prompt = CouncilPromptBuilder.Build(MinimalInput("Ajin"));

         prompt.Should().Contain("NO DEED IS SEALED AT THIS TABLE");
         prompt.Should().Contain("[QUEST]");
         prompt.Should().Contain("[QUEST_COMPLETE]");
         prompt.Should().Contain("[EVENT]");
         prompt.Should().Contain("no one emits a [QUEST],");
      }

      // The mod's ResolutionOfferingResolver gates each extra kind on live eligibility; a kind not passed here
      // must never be proposed, so it must not appear as available unless the caller actually offers it.
      [Test]
      public void GIVEN_offered_resolution_kinds_WHEN_building_the_prompt_THEN_they_are_listed()
      {
         var input = new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {new() {Name = "Ajin"}},
            OfferedResolutionKinds = new List<string> {"give_gold", "declare_war"}
         };

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("give_gold");
         prompt.Should().Contain("declare_war");
      }

      // The negative case: an empty offer list is the ordinary "only quest is available" turn, and must not
      // print a hollow "may also resolve:" header naming nothing.
      [Test]
      public void GIVEN_no_offered_resolution_kinds_WHEN_building_the_prompt_THEN_no_extra_kinds_header_appears()
      {
         string prompt = CouncilPromptBuilder.Build(MinimalInput("Ajin"));

         prompt.Should().NotContain("THIS COUNCIL MAY ALSO RESOLVE");
      }

      // The player's own line and the sitting's earlier turns are what makes the reply *this* turn's rather
      // than a generic one; both must reach the model.
      [Test]
      public void GIVEN_a_player_line_and_transcript_so_far_WHEN_building_the_prompt_THEN_both_appear()
      {
         var input = new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {new() {Name = "Ajin"}},
            PlayerLine = "What do you make of the raiders near Pravend?",
            PlayerName = "Derthert",
            TranscriptSoFar = new List<string> {"Ajin the Hawk: The roads grow unsafe, my lord."}
         };

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("What do you make of the raiders near Pravend?");
         prompt.Should().Contain("Ajin the Hawk: The roads grow unsafe, my lord.");
      }

      // World state grounds the scene (a sitting in high summer reads differently from one in a winter siege);
      // each clause the caller supplies must render, and an absent one must not print a stray label.
      [Test]
      public void GIVEN_world_state_WHEN_building_the_prompt_THEN_day_season_and_place_all_appear()
      {
         var input = new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {new() {Name = "Ajin"}},
            Day = 214,
            Season = "spring",
            Place = "the great hall of Pravend"
         };

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("day 214");
         prompt.Should().Contain("spring");
         prompt.Should().Contain("the great hall of Pravend");
      }

      // A roster member's persona, culture, and regard are what let the model voice them true to their own
      // nature rather than as an interchangeable placeholder.
      [Test]
      public void GIVEN_a_member_with_persona_culture_and_regard_WHEN_building_the_prompt_THEN_all_three_render()
      {
         var input = new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {
               new() {
                  Name = "Ajin the Hawk",
                  PersonaLine = "a blunt old marshal, proud of his own troops",
                  Culture = "Battanian",
                  RegardTowardPlayer = 12
               }
            }
         };

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("a blunt old marshal, proud of his own troops");
         prompt.Should().Contain("Battanian");
         prompt.Should().Contain("+12");
      }

      // A real player-reported bug: with no sex stated the model guessed one from the name alone and voiced a
      // male councillor as a woman. Each roster line must state the member's sex explicitly and unmissably,
      // right beside the name, so the model never has to guess it.
      [Test]
      public void GIVEN_a_male_and_a_female_member_WHEN_building_the_prompt_THEN_each_roster_line_states_their_sex()
      {
         var input = new CouncilPromptInput {
            Roster = new List<CouncilMemberInput> {
               new() {Name = "Ajin the Hawk", IsFemale = false},
               new() {Name = "Sophia", IsFemale = true}
            }
         };

         string prompt = CouncilPromptBuilder.Build(input);

         prompt.Should().Contain("Ajin the Hawk, a man");
         prompt.Should().Contain("Sophia, a woman");
      }

      // Cache economics: the stable head (framing, output format, the RESOLUTION/change_relation channels) must
      // be BYTE-FOR-BYTE identical whatever the roster or turn is, so a caching provider can pin it as a shared
      // prefix instead of re-billing the whole scaffold every council call.
      [Test]
      public void GIVEN_two_different_rosters_WHEN_building_both_prompts_THEN_they_share_the_same_stable_head()
      {
         string promptA = CouncilPromptBuilder.Build(MinimalInput("Ajin"));
         string promptB = CouncilPromptBuilder.Build(MinimalInput("Hophtalamos", "Sley", "Aldric"));

         promptA.Should().StartWith(CouncilPromptBuilder.StableHead);
         promptB.Should().StartWith(CouncilPromptBuilder.StableHead);
      }

      // An empty player line (the sitting's opening turn, before the player has said anything) must not print a
      // blank "Name: " line that reads as the player having spoken nothing.
      [Test]
      public void GIVEN_no_player_line_yet_WHEN_building_the_prompt_THEN_the_table_is_told_it_opens_on_its_own()
      {
         string prompt = CouncilPromptBuilder.Build(MinimalInput("Ajin"));

         prompt.Should().Contain("THE PLAYER HAS NOT YET SPOKEN THIS TURN: the table opens on its own.");
      }
   }
}
