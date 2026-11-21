// Code written by Gabriel Mailhot, 20/11/2026.
// What LlmMemoryCompressor keeps whatever the model says: a memory that carries a picture. Bhelogan (Discord, 20/11/2026)
// asked for pictures painted by his mod in the Memories panel, each attached to one memory; Gabriel (20/11/2026): "on
// protège les IDS associés à des images". If this breaks, a picture the player asked for is left hanging on a memory
// the next compression pass folded away, and the panel shows the painting of something the character forgot.

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
   public class PictureMemoryCompressionTests
   {
      // STAKE: the player had a memory painted; a month later compression runs and the model would drop it.
      [Test]
      public async Task GIVEN_an_old_memory_with_a_picture_WHEN_the_model_would_drop_everything_THEN_the_character_keeps_it()
      {
         CompressionResult result = await new LlmMemoryCompressor(new DropEverything()).CompressAsync(Npc("m1.png"));

         result.KeptEvents.Should().Contain(e => e.summary == "We fought side by side at Pendraic." && e.ImageFile == "m1.png");
      }

      // STAKE: compression still compresses. The same memory with no picture may go.
      [Test]
      public async Task GIVEN_the_same_memory_without_a_picture_WHEN_the_model_would_drop_everything_THEN_it_may_go()
      {
         CompressionResult result = await new LlmMemoryCompressor(new DropEverything()).CompressAsync(Npc(null));

         result.KeptEvents.Select(e => e.summary).Should().NotContain("We fought side by side at Pendraic.");
      }

      // STAKE: the picture and the conversation link follow a memory rewritten in place (the summary that enriches a
      // conversation's anchor uses a copy with a new text).
      [Test]
      public void GIVEN_a_memory_with_a_picture_and_a_conversation_WHEN_its_text_is_rewritten_THEN_both_follow()
      {
         var memory = new NotableEvent(10, NotableEventType.Other, "Spoke with Gabriel.") {ImageFile = "m1.png", SessionId = "s1"}.Identified();

         NotableEvent rewritten = memory with {summary = "Promised to ride to Pravend."};

         rewritten.ImageFile.Should().Be("m1.png");
         rewritten.SessionId.Should().Be("s1");
         rewritten.Id.Should().Be(memory.Id);
      }

      #region private

      // Fourteen memories (compression starts at twelve): the painted one first, a month back, then thirteen ordinary ones.
      private static NpcProfile Npc(string imageFile)
      {
         var npc = new NpcProfile {Id = "npc_test", Name = "Derthert", Faction = "Vlandia", Clan = "dey Meroc"};
         var events = new List<NotableEvent> {new(10, NotableEventType.Other, "We fought side by side at Pendraic.") {ImageFile = imageFile}};
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
