// Code written by Gabriel Mailhot, 23/06/2026.

#region

using System;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;

#endregion

namespace NpcMemoryService.Core.Services
{
   /// <summary>
   ///   Orchestrates a single NPC dialogue turn:
   ///   builds the prompt → calls the LLM → parses the response → updates the session.
   ///   Does not handle persistence; see INpcMemoryStore (Phase 4).
   /// </summary>
   public sealed class NpcChatService
   {
      private readonly ILlmClient _llmClient;
      private readonly IResponseParser _parser;

      public NpcChatService(
         ILlmClient llmClient,
         IResponseParser parser,
         IPromptBuilder promptBuilder)
      {
         _llmClient = llmClient;
         _parser = parser;
         PromptBuilder = promptBuilder;
      }

      /// <summary>
      ///   Generation settings for the chat turn. Larger MaxTokens allows the
      ///   LLM to develop the [DIALOGUE] section without being cut off; the
      ///   companion DIALOGUE STYLE directive in the system prompt teaches the
      ///   LLM what to do with that headroom.
      /// </summary>
      public LlmParameters ChatParameters { get; init; } = new() {
         MaxTokens = 1500,
         Creativity = 0.7f
      };

      public IPromptBuilder PromptBuilder { get; }

      /// <summary>
      ///   Sends the player's message to the NPC and returns a structured response.
      ///   The session is updated with the new turn automatically.
      ///   The NpcProfile is NOT mutated here; callers apply reputation/memory
      ///   changes using the returned <see cref="ParsedResponse" /> at their discretion.
      ///   <paramref name="encounterContext" /> is optional. When provided, the
      ///   volatile per-encounter state (scene, war status, player standing,
      ///   time since last meeting) is injected into the system prompt so the
      ///   LLM can adapt its tone without baking it into the persistent profile.
      /// </summary>
      public async Task<NpcChatResult> ChatAsync(
         NpcProfile npc,
         WorldState world,
         ChatSession session,
         string playerMessage,
         EncounterContext? encounterContext = null,
         CancellationToken ct = default)
      {
         session.AddPlayerMessage(playerMessage);

         string systemPrompt = PromptBuilder.BuildSystemPrompt(npc, world, encounterContext);

         // Prompt-cache split: everything up to the per-turn "CURRENT ENCOUNTER" block is stable within a
         // conversation (identity, persona, instructions) and makes the cacheable prefix; the dynamic tail
         // (encounter, rumours, names) is sent fresh. So the cache breakpoint survives the day/encounter
         // changing each turn instead of invalidating the whole system prompt.
         int splitAt = systemPrompt.IndexOf(NpcMemoryService.Core.Prompts.PromptBuilder.EncounterSectionHeading, StringComparison.Ordinal);
         string? stablePrefix = splitAt > 0
            ? systemPrompt.Substring(0, splitAt)
            : null;

         var request = new LlmRequest {
            SystemPrompt = systemPrompt,
            StableSystemPrompt = stablePrefix,
            Messages = session.Messages,
            Parameters = ChatParameters
         };

         LlmResponse llmResponse = await _llmClient
                                         .CompleteAsync(request, ct)
                                         .ConfigureAwait(false);

         if (!llmResponse.IsSuccess)
         {
            session.RollbackLastMessage();

            return NpcChatResult.Failure(llmResponse.ErrorMessage ?? "Unknown LLM error.");
         }

         ParsedResponse parsed = _parser.Parse(llmResponse.Content);
         session.AddNpcMessage(parsed.Dialogue);

         return NpcChatResult.Success(parsed, llmResponse.Usage, llmResponse.FinishReason, llmResponse.WasRetried);
      }

      /// <summary>
      ///   Sends the player's message to a transient commoner NPC and returns
      ///   a structured response. Uses <see cref="IPromptBuilder.BuildCommonerSystemPrompt" />
      ///   instead of the full NPC prompt — no events, no memory, no quests.
      ///   The session is updated with the new turn; callers must NOT persist
      ///   the synthetic profile to the store.
      /// </summary>
      public async Task<NpcChatResult> ChatCommonerAsync(
         NpcProfile profile,
         CommonsKnowledge knowledge,
         ChatSession session,
         string playerMessage,
         CancellationToken ct = default)
      {
         session.AddPlayerMessage(playerMessage);

         string systemPrompt = PromptBuilder.BuildCommonerSystemPrompt(profile, knowledge);

         var request = new LlmRequest {
            SystemPrompt = systemPrompt,
            Messages = session.Messages,
            Parameters = ChatParameters
         };

         LlmResponse llmResponse = await _llmClient
                                         .CompleteAsync(request, ct)
                                         .ConfigureAwait(false);

         if (!llmResponse.IsSuccess)
         {
            session.RollbackLastMessage();

            return NpcChatResult.Failure(llmResponse.ErrorMessage ?? "Unknown LLM error.");
         }

         ParsedResponse parsed = _parser.Parse(llmResponse.Content);
         session.AddNpcMessage(parsed.Dialogue);

         return NpcChatResult.Success(parsed, llmResponse.Usage, llmResponse.FinishReason, llmResponse.WasRetried);
      }
   }
}