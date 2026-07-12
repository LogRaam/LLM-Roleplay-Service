// Code written by Gabriel Mailhot, 11/07/2026.
// Live end-to-end guard for the v1.30.6 backstory regression fix. This actually injects a strong named
// persona ("Kratos style") into a real NpcChatService.ChatAsync call against OpenRouter, then uses a second
// real LLM call as an impartial JUDGE to verify the persona lands forcefully (not the diluted, generic voice
// the old "invent 2-3 subtle quirks" framing produced). Marked [Explicit] + [Category("Integration")] so it
// never runs in the default suite (it costs tokens and needs the network); run it on demand:
//   dotnet test --filter TestCategory=Integration
// Requires the OPENROUTER_API_KEY environment variable (same key the mod uses).

#region

using System.Threading.Tasks;
using FluentAssertions;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   [Explicit("Hits the real OpenRouter API; run on demand via --filter TestCategory=Integration")]
   [Category("Integration")]
   public class AuthoredBackstoryLiveLlmTests
   {
      private const string Model = "x-ai/grok-4.20";

      // A backstory that NAMES a strong figure but spells out no literal speech quirks: exactly the case the
      // v1.30.4 change broke (it invented faint generic tics instead of adopting the figure's full manner).
      private const string KratosBackstory =
         "You carry yourself like Kratos from God of War: a battle-scarred, rage-forged warrior. Your voice " +
         "is low, hard, and merciless. You speak in short, brutal, commanding sentences. You have no patience " +
         "for weakness or flattery, and a buried grief hardens every word you speak.";

      // The persona the reply must embody, described to the judge WITHOUT naming Kratos (the NPC must keep the
      // outside world's names out of Calradia, so we grade the voice, not any imported lore).
      private const string PersonaForJudge =
         "a battle-scarred, rage-forged warrior with a low, hard, merciless voice, who speaks in short, brutal, " +
         "commanding sentences, with no patience for weakness or flattery";

      private static string RequireApiKey()
      {
         string? key = System.Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
         if (string.IsNullOrWhiteSpace(key))
            Assert.Ignore("OPENROUTER_API_KEY not set; skipping live LLM test.");

         return key!;
      }

      private static OpenRouterClient BuildClient(string apiKey)
         => new(new System.Net.Http.HttpClient(),
            new OpenRouterConfig {ApiKey = apiKey, Model = Model});

      private static NpcProfile KratosNpc() => new() {
         Id = "npc_kratos_test",
         Name = "Bjorn the Grim",
         Faction = "Sturgia",
         Clan = "Vagiroving",
         AuthoredBackstory = KratosBackstory
      };

      [Test]
      public async Task GIVEN_a_strong_named_backstory_WHEN_a_real_llm_replies_THEN_an_impartial_judge_confirms_the_persona_lands()
      {
         string apiKey = RequireApiKey();
         OpenRouterClient client = BuildClient(apiKey);

         var chat = new NpcChatService(client, new SectionResponseParser(),
            new PromptBuilder {ActionVocabulary = new System.Collections.Generic.List<GameActionDefinition>()});

         // ── 1. Real in-character reply through the exact game pipeline (Full prompt: no EncounterContext) ──
         NpcChatResult result = await chat.ChatAsync(
            KratosNpc(),
            new WorldState {CurrentDay = 142, ActiveConflicts = "Sturgia is at war with the Western Empire."},
            new ChatSession(),
            "State who you are, and tell me why I should not fear you.");

         result.IsSuccess.Should().BeTrue($"the LLM call should succeed (error: {result.ErrorMessage})");
         string reply = result.Response!.Dialogue ?? "";
         await TestContext.Out.WriteLineAsync($"\n--- NPC REPLY ---\n{reply}\n");
         reply.Should().NotBeNullOrWhiteSpace();

         // ── 2. Second real LLM call as an impartial judge of whether the persona actually lands ──
         string verdict = await Judge(client, reply);
         await TestContext.Out.WriteLineAsync($"--- JUDGE ---\n{verdict}\n");

         verdict.TrimStart().ToUpperInvariant().Should().StartWith("YES",
            "the reply must forcefully embody the authored persona, not a diluted generic voice");
      }

      private static async Task<string> Judge(OpenRouterClient client, string reply)
      {
         string system =
            "You are a strict theatrical coach. You judge whether a single line of NPC dialogue clearly and " +
            $"forcefully embodies this persona: {PersonaForJudge}. Grade the VOICE and force of personality, " +
            "not vocabulary or any proper names. Answer with exactly one word first, YES or NO (YES only if the " +
            "persona is unmistakable and strong, NO if the line is generic, flat, polite, or weakly characterised), " +
            "then a short reason on the same line.";

         var request = new LlmRequest {
            SystemPrompt = system,
            Messages = new[] {new LlmMessage(MessageRole.User, $"NPC line to grade:\n\"{reply}\"")}
         };

         LlmResponse response = await client.CompleteAsync(request);
         response.IsSuccess.Should().BeTrue($"the judge call should succeed (error: {response.ErrorMessage})");

         return response.Content ?? "";
      }
   }
}
