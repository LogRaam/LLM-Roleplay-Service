// Code written by Gabriel Mailhot, 15/07/2026.
// Pins the save-healing half of the inline chain-of-thought fix. ReasoningTraceStripper stops NEW leaks at
// parse time, but a save made before the fix already stores the trace as the NPC's memory (the reported
// screenshot: "The user wants me to write a memory line from Mesui's perspective..."), and stored events are
// re-displayed and re-injected into every prompt forever. This backfill runs at every session launch, so it
// must be idempotent and must only report "changed" when it truly healed something, or every launch would
// re-persist every profile for nothing.

#region

using System.Collections.Generic;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class ReasoningTraceBackfillTests
   {
      // The half-corrupted shape: trace followed by the real memory line. The real prose must be
      // recovered, not thrown away with the trace, and the event's day and type must survive untouched.
      [Test]
      public void GIVEN_an_event_whose_summary_holds_a_trace_before_the_prose_WHEN_scrubbing_THEN_the_prose_is_recovered()
      {
         NpcProfile profile = ProfileWithSummaries("<think>Let me summarize the exchange.</think>Huan Yi kept his word about the horses.");

         ReasoningTraceBackfill.Scrub(profile).Should().BeTrue();

         profile.Events[0].summary.Should().Be("Huan Yi kept his word about the horses.");
         profile.Events[0].gameDay.Should().Be(91090);
         profile.Events[0].type.Should().Be(NotableEventType.Collaboration);
      }

      // The reported screenshot: the whole stored summary is a truncated trace, no prose ever followed.
      // Dropping the event would erase real history (its day and type happened); inventing content would
      // be guessing. The modest "faded" line keeps the record honest without either.
      [Test]
      public void GIVEN_an_event_whose_summary_is_pure_truncated_trace_WHEN_scrubbing_THEN_it_becomes_the_faded_memory_line()
      {
         NpcProfile profile = ProfileWithSummaries(
            "<think>The user wants me to write a memory line from Mesui's perspective. Let me summarize: 1. Huan Yi");

         ReasoningTraceBackfill.Scrub(profile).Should().BeTrue();

         profile.Events[0].summary.Should().Be(ReasoningTraceBackfill.LostMemoryLine);
      }

      // The overwhelmingly common case: a clean profile. Reporting "changed" here would make the host
      // re-persist every profile on every launch, which is exactly the churn the return value exists to avoid.
      [Test]
      public void GIVEN_a_clean_profile_WHEN_scrubbing_THEN_nothing_changes_and_nothing_needs_persisting()
      {
         NpcProfile profile = ProfileWithSummaries("Huan Yi delivered three sumpter horses, completing the quest.");
         profile.BackgroundContext = "We met at the Baltakhand horse market years ago.";

         ReasoningTraceBackfill.Scrub(profile).Should().BeFalse();

         profile.Events[0].summary.Should().Be("Huan Yi delivered three sumpter horses, completing the quest.");
         profile.BackgroundContext.Should().Be("We met at the Baltakhand horse market years ago.");
      }

      // BackgroundContext is prose compressed FROM events, so a trace can have leaked there too. Unlike an
      // event it carries no date worth preserving: a paragraph that was all trace goes back to plain
      // "no background context" (null) instead of a filler line.
      [Test]
      public void GIVEN_a_background_context_that_is_pure_trace_WHEN_scrubbing_THEN_it_returns_to_null()
      {
         NpcProfile profile = ProfileWithSummaries("A clean memory.");
         profile.BackgroundContext = "<think>I need to compress these older events into a paragraph";

         ReasoningTraceBackfill.Scrub(profile).Should().BeTrue();

         profile.BackgroundContext.Should().BeNull();
      }

      // The backfill runs on EVERY session launch with no version marker; only idempotence makes that safe.
      // The healed text (including the faded line) must itself scrub as already-clean on the next launch.
      [Test]
      public void GIVEN_a_profile_already_healed_WHEN_scrubbing_again_THEN_the_second_pass_changes_nothing()
      {
         NpcProfile profile = ProfileWithSummaries(
            "<think>trace</think>The real memory.",
            "<think>a trace cut off with no prose after it");
         ReasoningTraceBackfill.Scrub(profile).Should().BeTrue();

         ReasoningTraceBackfill.Scrub(profile).Should().BeFalse();

         profile.Events[0].summary.Should().Be("The real memory.");
         profile.Events[1].summary.Should().Be(ReasoningTraceBackfill.LostMemoryLine);
      }

      #region private

      private static NpcProfile ProfileWithSummaries(params string[] summaries)
      {
         var events = new List<NotableEvent>();
         foreach (string summary in summaries)
            events.Add(new NotableEvent(gameDay: 91090, NotableEventType.Collaboration, summary));

         return new NpcProfile {Id = "npc_test", Name = "Mesui", Faction = "Khuzait", Clan = "Kherit", Events = events};
      }

      #endregion
   }
}
