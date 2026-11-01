// Code written by Gabriel Mailhot, 31/10/2026.
// RaveN017Mod (Nexus PM, 29/10/2026): threat letters "appear in English even though my game is in French". The letters the
// mod writes from a fixed text are translated when opened. This pins what the model is told and what a failed call gives: if
// it is wrong, a letter comes back in the wrong language or gender, a blackmail's sum moves, or a failed call loses the letter.

#region

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class FixedLetterTranslatorPromptTests
   {
      private static FixedLetterTranslation Threat(string language = "French") => new() {
         Text = "Send me 5000 denars.", Language = language, WriterName = "Lucon", WriterIsFemale = false, ReaderIsFemale = true
      };

      // STAKE: the report itself. The language must be named, and the letter must be what is translated, or the player reads
      // English again or a model's guess at the letter.
      [Test]
      public async Task GIVEN_a_letter_and_a_language_WHEN_it_is_translated_THEN_the_model_gets_that_letter_and_that_language()
      {
         var client = new CapturingLlmClient();

         await new FixedLetterTranslator(client).TranslateAsync(Threat());

         client.LastRequest!.SystemPrompt.Should().Contain("into French");
         client.LastRequest.Messages.Should().ContainSingle(m => m.Content == "Send me 5000 denars.");
      }

      // STAKE: M-G6, the deadline in a blackmail's prose must match the real mechanic. A translation that rounds a sum or
      // spells out a delay tells the player something the game will not do.
      [Test]
      public async Task GIVEN_a_letter_with_a_sum_WHEN_it_is_translated_THEN_the_model_is_told_numbers_stay_in_digits()
      {
         var client = new CapturingLlmClient();

         await new FixedLetterTranslator(client).TranslateAsync(Threat());

         client.LastRequest!.SystemPrompt.Should().Contain("Every number stays exactly as it is written, in digits");
      }

      // STAKE: French and most languages the game ships agree a first person and a "you" with sex. A lord writing of himself
      // in the feminine, or to a lady in the masculine, is a mistake the English text never had.
      [Test]
      public async Task GIVEN_a_man_writing_to_a_woman_WHEN_it_is_translated_THEN_the_model_knows_both()
      {
         var client = new CapturingLlmClient();

         await new FixedLetterTranslator(client).TranslateAsync(Threat());

         client.LastRequest!.SystemPrompt.Should().Contain("written by Lucon, a man, to a woman");
      }

      // STAKE: Gabriel's approval rests on this: a model that does not answer must not cost the letter. Null is what tells
      // the host to keep the English text.
      [Test]
      public async Task GIVEN_a_model_that_fails_WHEN_a_letter_is_translated_THEN_no_text_comes_back_and_the_host_keeps_its_own()
      {
         var client = new CapturingLlmClient {Answer = new LlmResponse {IsSuccess = false, Content = ""}};

         string? text = await new FixedLetterTranslator(client).TranslateAsync(Threat());

         text.Should().BeNull();
      }

      // STAKE: no language means no call: the translator is never to spend a call on a letter it cannot place.
      [Test]
      public async Task GIVEN_no_language_WHEN_a_letter_is_translated_THEN_no_call_is_made()
      {
         var client = new CapturingLlmClient();

         await new FixedLetterTranslator(client).TranslateAsync(Threat(""));

         client.LastRequest.Should().BeNull();
      }

      #region private

      private sealed class CapturingLlmClient : ILlmClient
      {
         public LlmResponse Answer { get; init; } = new() {IsSuccess = true, Content = "Envoyez-moi 5000 deniers."};

         public LlmRequest? LastRequest { get; private set; }

         public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct = default)
         {
            LastRequest = request;

            return Task.FromResult(Answer);
         }
      }

      #endregion
   }
}
