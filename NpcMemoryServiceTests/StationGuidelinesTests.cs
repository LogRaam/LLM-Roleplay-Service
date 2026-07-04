// Code written by Gabriel Mailhot, 03/07/2026.
// Station-matched behavior guidelines: a gang leader, notable, or wanderer gets a voice register fit to
// their station instead of inheriting the lordly default (the "gang leader rising from her throne" bug).
// EncounterContext.StationGuidelinesOverride outranks the global BehaviorGuidelinesOverride, which
// outranks the built-in noble text.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class StationGuidelinesTests
   {
      private const string StationMarker = "UNIQUE_STATION_REGISTER_MARKER";
      private const string GlobalMarker = "UNIQUE_GLOBAL_GUIDELINES_MARKER";

      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Test Gang Leader",
         Faction = "Vlandia",
         Clan = ""
      };

      [Test]
      public void GIVEN_a_station_override_WHEN_building_a_full_prompt_THEN_it_replaces_the_noble_guidelines()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, StationGuidelinesOverride = StationMarker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(StationMarker);
         prompt.Should().NotContain("how a noble of");
      }

      [Test]
      public void GIVEN_a_station_override_WHEN_a_global_override_also_exists_THEN_the_station_wins()
      {
         var builder = new PromptBuilder {BehaviorGuidelinesOverride = GlobalMarker};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, StationGuidelinesOverride = StationMarker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(StationMarker);
         prompt.Should().NotContain(GlobalMarker);
      }

      [Test]
      public void GIVEN_no_station_override_WHEN_a_global_override_exists_THEN_the_global_still_applies()
      {
         var builder = new PromptBuilder {BehaviorGuidelinesOverride = GlobalMarker};
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(GlobalMarker);
      }

      [Test]
      public void GIVEN_a_lean_prompt_WHEN_a_station_override_is_supplied_THEN_it_still_replaces_the_lordly_line()
      {
         // The lean fallback opens with "You speak as a lord of ..." — a station override must displace
         // it at every prompt size, or small-model users keep the bug.
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Lean, StationGuidelinesOverride = StationMarker};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain(StationMarker);
         prompt.Should().NotContain("You speak as a lord of");
      }

      [Test]
      public void GIVEN_a_blank_station_override_WHEN_building_THEN_the_normal_fallback_chain_applies()
      {
         var builder = new PromptBuilder();
         var context = new EncounterContext {LeanLevel = LeanPromptLevel.Full, StationGuidelinesOverride = "   "};

         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

         prompt.Should().Contain("how a noble of");
      }
   }
}
