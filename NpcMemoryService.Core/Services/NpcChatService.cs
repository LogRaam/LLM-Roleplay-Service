using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;

namespace NpcMemoryService.Core.Services
{
    /// <summary>
    /// Orchestrates a single NPC dialogue turn:
    /// builds the prompt → calls the LLM → parses the response → updates the session.
    /// Does not handle persistence; see INpcMemoryStore (Phase 4).
    /// </summary>
    public sealed class NpcChatService
    {
        private readonly ILlmClient     _llmClient;
        private readonly IResponseParser _parser;
        private readonly IPromptBuilder  _promptBuilder;

        public NpcChatService(
            ILlmClient      llmClient,
            IResponseParser  parser,
            IPromptBuilder   promptBuilder)
        {
            _llmClient     = llmClient;
            _parser        = parser;
            _promptBuilder = promptBuilder;
        }

        /// <summary>
        /// Sends the player's message to the NPC and returns a structured response.
        /// The session is updated with the new turn automatically.
        /// The NpcProfile is NOT mutated here; callers apply reputation/memory
        /// changes using the returned <see cref="ParsedResponse"/> at their discretion.
        /// </summary>
        public async Task<NpcChatResult> ChatAsync(
            NpcProfile      npc,
            WorldState      world,
            ChatSession     session,
            string          playerMessage,
            CancellationToken ct = default)
        {
            session.AddPlayerMessage(playerMessage);

            string systemPrompt = _promptBuilder.BuildSystemPrompt(npc, world);

            var request = new LlmRequest
            {
                SystemPrompt = systemPrompt,
                Messages     = session.Messages
            };

            LlmResponse llmResponse = await _llmClient
                .CompleteAsync(request, ct)
                .ConfigureAwait(false);

            if (!llmResponse.IsSuccess)
            {
                // Remove the player message we just added so the session
                // stays consistent with what the NPC has actually seen.
                session.RollbackLastMessage();
                return NpcChatResult.Failure(llmResponse.ErrorMessage ?? "Unknown LLM error.");
            }

            ParsedResponse parsed = _parser.Parse(llmResponse.Content);

            session.AddNpcMessage(parsed.Dialogue);

            return NpcChatResult.Success(parsed, llmResponse.Usage);
        }
    }
}
