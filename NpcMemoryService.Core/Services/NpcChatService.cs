// Code written by Gabriel Mailhot, 24/05/2026.

#region

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
        public LlmParameters ChatParameters { get; init; } = new LlmParameters {
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

            var systemPrompt = PromptBuilder.BuildSystemPrompt(npc, world, encounterContext);

            LlmRequest request = new LlmRequest {
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

            return NpcChatResult.Success(parsed, llmResponse.Usage);
        }
    }
}