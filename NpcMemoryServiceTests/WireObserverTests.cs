// Code written by Gabriel Mailhot, 15/09/2026.
// What these pin: that the seat beside the wire reports the wire, and costs nothing when empty.
//
// The observer exists because fkasad had to find a cache defect from the outside, by hand, with no hook
// (Nexus, 15/09/2026). Two properties make it worth having and both are pinned here: it must describe
// the request AS SENT - the same split the client actually applies, not a second opinion about it - and
// it must never be able to hurt a conversation, however badly it is written.

#region

using System;
using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class WireObserverTests
   {
      private const string Stable = "STABLE PREFIX. ";
      private const string Tail = "CURRENT ENCOUNTER: a keep, at dusk.";

      private sealed class Recorder : ILlmWireObserver
      {
         public readonly List<LlmWireSnapshot> Seen = new();

         public void OnRequest(LlmWireSnapshot snapshot) => Seen.Add(snapshot);
      }

      private sealed class Saboteur : ILlmWireObserver
      {
         public void OnRequest(LlmWireSnapshot snapshot) => throw new InvalidOperationException("badly written observer");
      }

      private static OpenRouterClient Client(bool caching = true)
         => new(new HttpClient(),
                new OpenRouterConfig {
                   ApiKey = "k", Model = "test/model", UseSystemPromptCachingProvider = () => caching
                });

      private static LlmRequest Request(string prefix = Stable, string tail = Tail)
         => new() {
            SystemPrompt = prefix + tail,
            StableSystemPrompt = prefix,
            Messages = new List<LlmMessage> {new(MessageRole.User, "Good evening.")},
            Parameters = new LlmParameters()
         };

      // The observer must describe the request AS SENT: the prefix it reports is the one that actually
      // carried the breakpoint, and the tail is the remainder, not a re-derivation of either.
      [Test]
      public void GIVEN_an_observer_WHEN_a_request_is_built_THEN_it_sees_the_split_that_was_actually_sent()
      {
         var recorder = new Recorder();
         OpenRouterClient client = Client();
         client.WireObserver = recorder;

         InvokeBuild(client, Request());

         recorder.Seen.Should().HaveCount(1);
         recorder.Seen[0].StablePrefix.Should().Be(Stable);
         recorder.Seen[0].DynamicTail.Should().Be(Tail);
         recorder.Seen[0].SystemChars.Should().Be(Stable.Length + Tail.Length);
         recorder.Seen[0].CachingRequested.Should().BeTrue();
         recorder.Seen[0].Model.Should().Be("test/model");
      }

      // Caching off is a real configuration, not a broken one, and the report must say so rather than
      // claim a prefix that no breakpoint was placed on.
      [Test]
      public void GIVEN_caching_is_off_WHEN_a_request_is_built_THEN_no_prefix_is_claimed()
      {
         var recorder = new Recorder();
         OpenRouterClient client = Client(caching: false);
         client.WireObserver = recorder;

         InvokeBuild(client, Request());

         recorder.Seen[0].CachingRequested.Should().BeFalse();
         recorder.Seen[0].StablePrefix.Should().BeNull();
         recorder.Seen[0].DynamicTail.Should().Be(Stable + Tail, "with no breakpoint the whole prompt is sent fresh");
      }

      // The conversation is NOT behind the breakpoint, and its size is the number that grows as a
      // conversation runs long - which is the next thing worth measuring after the prefix.
      [Test]
      public void GIVEN_a_conversation_WHEN_a_request_is_built_THEN_the_message_weight_is_reported_apart_from_the_prompt()
      {
         var recorder = new Recorder();
         OpenRouterClient client = Client();
         client.WireObserver = recorder;

         InvokeBuild(client, Request());

         recorder.Seen[0].MessageCount.Should().Be(1);
         recorder.Seen[0].MessageChars.Should().Be("Good evening.".Length);
      }

      // THE PROPERTY THAT MATTERS MOST. A diagnostic must never be able to fail a player's conversation,
      // so an observer that throws is swallowed and the request is built regardless.
      [Test]
      public void GIVEN_an_observer_that_throws_WHEN_a_request_is_built_THEN_the_request_is_built_anyway()
      {
         OpenRouterClient client = Client();
         client.WireObserver = new Saboteur();

         Action build = () => InvokeBuild(client, Request());

         build.Should().NotThrow();
      }

      // No observer, no work: the default is silence, and it must stay reachable without configuration.
      [Test]
      public void GIVEN_no_observer_WHEN_a_request_is_built_THEN_nothing_happens()
      {
         OpenRouterClient client = Client();

         client.WireObserver.Should().BeNull();
         ((Action) (() => InvokeBuild(client, Request()))).Should().NotThrow();
      }

      #region private

      /// <summary>
      ///   Drives the private request builder, which is where the hook lives — deliberately, because that
      ///   is the one place the body exists as the bytes that will be sent.
      /// </summary>
      private static void InvokeBuild(OpenRouterClient client, LlmRequest request)
         => typeof(OpenRouterClient)
            .GetMethod("BuildHttpRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(client, new object[] {request});

      #endregion
   }

   [TestFixture]
   public sealed class PromptCacheReportTests
   {
      // The healthy steady state: turn three finds turn two's prefix untouched.
      [Test]
      public void GIVEN_an_unchanged_prefix_WHEN_compared_THEN_the_whole_of_it_is_reused()
      {
         PrefixComparison r = PromptCacheReport.Compare("IDENTITY AND RULES", "IDENTITY AND RULES");

         r.Verdict.Should().Be(PrefixVerdict.Whole);
         r.Reuse.Should().Be(1);
         r.DivergesAt.Should().Be(-1);
      }

      // THE DEFECT, as the player measured it: a divergence near the top discards nearly all of the prefix.
      [Test]
      public void GIVEN_a_prefix_that_moved_early_WHEN_compared_THEN_it_names_where_and_how_much_died()
      {
         PrefixComparison r = PromptCacheReport.Compare("ABCdefghij", "ABXdefghij");

         r.Verdict.Should().Be(PrefixVerdict.Broken);
         r.DivergesAt.Should().Be(2);
         r.Reuse.Should().BeApproximately(0.2, 0.001);
         r.ToString().Should().Contain("80.0% discarded");
      }

      // A first request is not a fault, and must not read as one in a log.
      [Test]
      public void GIVEN_the_first_request_WHEN_compared_THEN_it_is_reported_as_a_write_not_a_loss()
      {
         PrefixComparison r = PromptCacheReport.Compare(null, "IDENTITY AND RULES");

         r.Verdict.Should().Be(PrefixVerdict.NoPrevious);
         r.PrefixChars.Should().Be("IDENTITY AND RULES".Length);
         r.ToString().Should().Contain("first request");
      }

      // No marker, no split — and the line must say what that COSTS, since it is the silent version of the
      // same defect: the whole prompt becomes the cached block and every per-turn change busts all of it.
      [Test]
      public void GIVEN_a_prompt_that_was_never_split_WHEN_compared_THEN_the_report_says_what_it_costs()
      {
         PrefixComparison r = PromptCacheReport.Compare("anything", null);

         r.Verdict.Should().Be(PrefixVerdict.NotSplit);
         r.ToString().Should().Contain("every turn will bust it");
      }

      // A prefix that only GREW still broke: the provider matches from the start and stops at the first
      // difference, so a longer prefix sharing the shorter one entirely is not a whole reuse.
      [Test]
      public void GIVEN_a_prefix_that_grew_WHEN_compared_THEN_it_is_not_counted_as_whole()
      {
         PrefixComparison r = PromptCacheReport.Compare("IDENTITY", "IDENTITY AND MORE");

         r.Verdict.Should().Be(PrefixVerdict.Broken);
         r.SharedChars.Should().Be("IDENTITY".Length);
      }
   }
}
