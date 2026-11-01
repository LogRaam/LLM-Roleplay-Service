// Code written by Gabriel Mailhot, 31/10/2026.
// RaveN017Mod (Nexus PM, 29/10/2026): threat letters "appear in English even though my game is in French". A few letters are
// fixed texts the mod writes itself, with no model call, so Reply Language never reached them. Gabriel approved translating
// them (31/10/2026, "go"). A translation and not a fresh letter: a blackmail's sum and deadline are the real mechanic's.

#region

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;

#endregion

namespace NpcMemoryService.Core.Services
{
   /// <summary>One fixed letter to translate, and what its grammar needs to know.</summary>
   public sealed class FixedLetterTranslation
   {
      /// <summary>The letter's English text.</summary>
      public string Text { get; init; } = "";

      /// <summary>The language to translate it into.</summary>
      public string Language { get; init; } = "";

      /// <summary>The writer's name, kept as it is in the translation.</summary>
      public string WriterName { get; init; } = "";

      /// <summary>The writer is a woman; null when unknown. Many languages agree a first person with it.</summary>
      public bool? WriterIsFemale { get; init; }

      /// <summary>The reader is a woman; null when unknown. Many languages agree "you" with it.</summary>
      public bool? ReaderIsFemale { get; init; }
   }

   /// <summary>
   ///   Translates a letter the mod wrote from a fixed text into the player's reply language, via a single LLM call. Returns
   ///   null on failure, so the caller keeps the English text.
   /// </summary>
   public sealed class FixedLetterTranslator
   {
      private readonly ILlmClient _llmClient;

      public FixedLetterTranslator(ILlmClient llmClient)
      {
         _llmClient = llmClient;
      }

      public LlmParameters Parameters { get; init; } = new() {
         MaxTokens = 600,
         Creativity = 0.3f,
         ReasoningOverride = "off" // a short translation, never benefits from reasoning
      };

      /// <summary>The translated letter, or null when the model did not answer.</summary>
      public async Task<string?> TranslateAsync(FixedLetterTranslation letter, CancellationToken ct = default)
      {
         if (string.IsNullOrWhiteSpace(letter.Text) || string.IsNullOrWhiteSpace(letter.Language)) return null;

         var request = new LlmRequest {
            SystemPrompt = BuildSystemPrompt(letter),
            Messages = [new LlmMessage(MessageRole.User, letter.Text)],
            Parameters = Parameters
         };

         LlmResponse? response = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);

         if (response is not {IsSuccess: true} || string.IsNullOrWhiteSpace(response.Content))
            return null;

         return response.Content.Trim();
      }

      #region private

      private static string BuildSystemPrompt(FixedLetterTranslation letter)
      {
         string language = letter.Language.Trim();
         string writer = letter.WriterIsFemale switch {
            true => $"{letter.WriterName}, a woman",
            false => $"{letter.WriterName}, a man",
            _ => letter.WriterName
         };
         string reader = letter.ReaderIsFemale switch {
            true => "a woman",
            false => "a man",
            _ => "someone of unknown sex"
         };

         var sb = new StringBuilder();
         sb.AppendLine($"You translate letters of {PromptLore.WorldName}, a medieval world, into {language}.");
         sb.AppendLine($"The letter below was written by {writer}, to {reader}. Make the grammar of both agree with that.");
         sb.AppendLine($"Translate ALL of it into natural, period-appropriate {language}, in the same tone and the same voice.");
         sb.AppendLine("Add nothing and leave nothing out.");
         sb.AppendLine("Every number stays exactly as it is written, in digits. A sum or a delay in this letter is binding.");
         sb.AppendLine("Proper names of people and places keep their spelling.");
         sb.AppendLine("No preamble, no quotation marks, no notes: write only the translated letter.");

         return sb.ToString();
      }

      #endregion
   }
}
