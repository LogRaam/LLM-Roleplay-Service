// Code written by Gabriel Mailhot, 15/11/2026.
// What LlmMemoryCompressor keeps whatever the model says: a memory the player wrote by hand. Icezera (Discord,
// 15/11/2026, "Memories added using cr.memory_add disappear after some time"): "I was editing and adding some
// memories to characters. Later on, I conversed with the NPC's in question and realized they weren't remembering what
// I put down." A hand-written memory is stored as an ordinary event, usually dated in the past, so it sits far from the
// five most recent that compression always keeps, and the model could fold it away. If this breaks, the past a player
// writes for a character is quietly lost the next time that character's memory is compressed.

#region

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.Compression;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PlayerWrittenMemoryCompressionTests
   {
      // THE REPORT: a memory written with cr.memory_add, dated a month back, vanished after a compression pass.
      [Test]
      public async Task GIVEN_a_memory_the_player_wrote_long_ago_WHEN_the_model_would_drop_everything_THEN_the_character_keeps_it()
      {
         NpcProfile npc = Npc(withPlayerWritten: true);

         CompressionResult result = await new LlmMemoryCompressor(new DropEverything()).CompressAsync(npc);

         result.KeptEvents.Select(e => e.summary).Should().Contain("We fought side by side at Pendraic.");
      }

      // STAKE: compression still compresses. An ordinary memory of the same age, not written by the player, can go.
      [Test]
      public async Task GIVEN_an_ordinary_memory_of_the_same_age_WHEN_the_model_would_drop_everything_THEN_it_may_go()
      {
         NpcProfile npc = Npc(withPlayerWritten: false);

         CompressionResult result = await new LlmMemoryCompressor(new DropEverything()).CompressAsync(npc);

         result.KeptEvents.Select(e => e.summary).Should().NotContain("We fought side by side at Pendraic.");
      }

      #region private

      // Fourteen memories (compression starts at twelve): the hand-written one first, dated a month back, then thirteen
      // ordinary ones after it.
      private static NpcProfile Npc(bool withPlayerWritten)
      {
         var npc = new NpcProfile {Id = "npc_test", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};
         var events = new List<NotableEvent> {new(10, NotableEventType.Other, "We fought side by side at Pendraic.") {IsPlayerWritten = withPlayerWritten}};
         for (var day = 20; day < 33; day++) events.Add(new NotableEvent(day, NotableEventType.Other, $"Spoke of the harvest on day {day}."));
         npc.ReplaceEvents(events);

         return npc;
      }

      private sealed class DropEverything : ILlmClient
      {
         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
            => Task.FromResult(new LlmResponse {Content = "[KEEP]\n[/KEEP]\n[DROP_SUMMARY]Old talk.[/DROP_SUMMARY]", IsSuccess = true});
      }

      #endregion
   }
}
