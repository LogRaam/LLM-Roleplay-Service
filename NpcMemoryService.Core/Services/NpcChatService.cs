// Code written by Gabriel Mailhot, 23/06/2026.

#region

using System;
using System.Collections.Generic;
using System.Linq;
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
         MaxTokens = 3000,
         Creativity = 0.7f
      };

      public IPromptBuilder PromptBuilder { get; }

      /// <summary>
      ///   The context window of the model this service talks to, in tokens, read on every turn; 0 (or no provider) when
      ///   none is declared. When set, each request is fitted to it by CR (ContextFitPolicy) instead of being cut by the
      ///   server (fkasad, 13/10/2026: LM Studio's "truncate middle" cut the action rules out of CR's prompt).
      /// </summary>
      public Func<int>? ContextWindowProvider { get; init; }

      /// <summary>Raised after every fitted request, so the host can log it and tell the player.</summary>
      public static event Action<ContextFitReport>? ContextFitted;

      // Characters per token on this endpoint, measured on the last reply the server counted.
      private double _charsPerToken = ContextFitPolicy.DefaultCharsPerToken;

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
         CancellationToken ct = default,
         bool allowTruncationRetry = true,
         string? modelOverride = null)
      {
         session.AddPlayerMessage(playerMessage);

         string systemPrompt = PromptBuilder.BuildSystemPrompt(npc, world, encounterContext);

         // Prompt-cache split: everything up to the per-turn "CURRENT ENCOUNTER" block is stable within a
         // conversation (identity, persona, instructions) and makes the cacheable prefix; the dynamic tail
         // (encounter, rumours, names) is sent fresh. So the cache breakpoint survives the day/encounter
         // changing each turn instead of invalidating the whole system prompt.
         string? stablePrefix = PromptCacheSplit.StablePrefixOf(systemPrompt);

         // allowTruncationRetry defaults true, so every existing caller (Integrated chat, captive/commoner/escape
         // turns) sends ChatParameters unchanged, byte-for-byte. Only the Prose + Interpreter mod flow sets it
         // false, for its PROSE call only, to fail fast on a truncated reply instead of paying for the client's
         // own bigger-budget retry (see LlmParameters.AllowTruncationRetry).
         // COMPACT ALSO BUDGETS THE REPLY, and this was the lever nobody had pulled. A local server counts the
         // reply budget against the SAME window as the prompt, so on an 8,192-token model a 3,000-token
         // MaxTokens spends over a third of the context before the character says a word — and it buys
         // nothing there, because the replies a small model actually writes are a few hundred tokens (250 to
         // ~1,000 across Gabriel's own sessions). A thousand is ample for the longest of those, and it frees
         // two thousand tokens: more headroom than every trim to the prompt TEXT put together, and it takes
         // no sentence away from anybody (DuskSymphony, Nexus, 8,192-token local model).
         int maxTokens = encounterContext?.LeanLevel == LeanPromptLevel.Lean
            ? Math.Min(ChatParameters.MaxTokens, LeanReplyBudget.Tokens)
            : ChatParameters.MaxTokens;

         IReadOnlyList<LlmMessage> messages = session.Messages;
         int window = DeclaredWindow();
         if (window > 0)
         {
            FittedRequest fitted = FitToWindow(window, npc, world, encounterContext, systemPrompt, session, maxTokens);
            systemPrompt = fitted.SystemPrompt;
            stablePrefix = PromptCacheSplit.StablePrefixOf(systemPrompt);
            messages = fitted.Messages;
            maxTokens = fitted.MaxTokens;
            allowTruncationRetry = allowTruncationRetry && fitted.RetryFits;
         }

         LlmParameters parameters = allowTruncationRetry && maxTokens == ChatParameters.MaxTokens
            ? ChatParameters
            : new LlmParameters {
               MaxTokens = maxTokens,
               Creativity = ChatParameters.Creativity,
               PresencePenalty = ChatParameters.PresencePenalty,
               AllowTruncationRetry = allowTruncationRetry
            };

         var request = new LlmRequest {
            SystemPrompt = systemPrompt,
            StableSystemPrompt = stablePrefix,
            // Diagnostic only (see LlmRequest.Subject): lets a wire observer compare this character's prefix
            // against HIS OWN previous one. A room of witnesses interleaves several speakers on one model.
            Subject = npc?.Id,
            Messages = messages,
            Parameters = parameters,
            // Null by default, exactly as before this parameter existed (the client resolves its own model).
            // Non-null lets a single call (the mod's Prose Fallback: redo this turn on a reliable explicit
            // model) target a model other than whatever the client is normally configured for.
            ModelOverride = modelOverride
         };

         LlmResponse llmResponse = await _llmClient
                                         .CompleteAsync(request, ct)
                                         .ConfigureAwait(false);

         if (!llmResponse.IsSuccess)
         {
            session.RollbackLastMessage();

            return NpcChatResult.Failure(llmResponse.ErrorMessage ?? "Unknown LLM error.");
         }

         if (window > 0)
         {
            double? measured = ContextFitPolicy.Measured(systemPrompt.Length + messages.Sum(m => m.Content?.Length ?? 0), llmResponse.Usage?.PromptTokens ?? 0);
            if (measured.HasValue) _charsPerToken = measured.Value;
         }

         ParsedResponse parsed = _parser.Parse(llmResponse.Content);
         session.AddNpcMessage(parsed.Dialogue);

         return NpcChatResult.Success(parsed, llmResponse.Usage, llmResponse.FinishReason, llmResponse.WasRetried);
      }

      /// <summary>
      ///   Fits one request to the declared window (ContextFitPolicy): the oldest raw lines go first, the running recap
      ///   and the latest lines stay; if the full prompt still does not fit with them, the compact prompt (and its
      ///   smaller reply budget) takes its place. The prompt itself is never cut: whole or compact.
      /// </summary>
      private FittedRequest FitToWindow(int window, NpcProfile npc, WorldState world, EncounterContext? context, string systemPrompt, ChatSession session, int maxTokens)
      {
         bool alreadyCompact = context?.LeanLevel == LeanPromptLevel.Lean;
         ContextFit fit = FitOf(window, systemPrompt, maxTokens, session, alreadyCompact ? 1 : ContextFitPolicy.KeepWithFullPrompt);
         bool switched = false;

         if (!fit.Fits && !alreadyCompact && context != null)
         {
            systemPrompt = PromptBuilder.BuildSystemPrompt(npc, world, context.WithLeanLevel(LeanPromptLevel.Lean));
            maxTokens = Math.Min(maxTokens, LeanReplyBudget.Tokens);
            fit = FitOf(window, systemPrompt, maxTokens, session, 1);
            switched = true;
         }
         else if (!fit.Fits)
            fit = FitOf(window, systemPrompt, maxTokens, session, 1);

         Report(npc?.Id, window, fit, switched);

         return new FittedRequest(systemPrompt, Kept(session, fit), maxTokens, ContextFitPolicy.RetryFits(fit, maxTokens));
      }

      // The session's messages with the fit's oldest raw lines left out (the pinned recap stays).
      private static IReadOnlyList<LlmMessage> Kept(ChatSession session, ContextFit fit)
      {
         IReadOnlyList<LlmMessage> all = session.Messages;
         int pinned = Math.Min(session.PinnedLeading, all.Count);

         return all.Take(pinned).Concat(all.Skip(pinned + fit.Drop)).ToList();
      }

      private void Report(string? subject, int window, ContextFit fit, bool switched)
         => ContextFitted?.Invoke(new ContextFitReport {
            Subject = subject, WindowTokens = window, EstimatedTokens = fit.EstimatedTokens, UsableTokens = fit.UsableTokens,
            DroppedMessages = fit.Drop, SwitchedToCompact = switched, Fits = fit.Fits, CharsPerToken = _charsPerToken
         });

      private ContextFit FitOf(int window, string systemPrompt, int maxTokens, ChatSession session, int keepAtLeast)
         => ContextFitPolicy.Fit(new ContextFitFacts {
            WindowTokens = window, CharsPerToken = _charsPerToken, PromptChars = systemPrompt?.Length ?? 0, ReplyTokens = maxTokens,
            Messages = session.Messages.Select(m => new ContextFitMessage {IsUser = m.Role == MessageRole.User, Chars = m.Content?.Length ?? 0}).ToList(),
            PinnedLeading = session.PinnedLeading, KeepAtLeast = keepAtLeast
         });

      private int DeclaredWindow()
      {
         try { return Math.Max(0, ContextWindowProvider?.Invoke() ?? 0); }
         catch
         {
            return 0; // a provider that fails declares nothing: the request goes as built
         }
      }

      private sealed class FittedRequest
      {
         public FittedRequest(string systemPrompt, IReadOnlyList<LlmMessage> messages, int maxTokens, bool retryFits)
         {
            SystemPrompt = systemPrompt;
            Messages = messages;
            MaxTokens = maxTokens;
            RetryFits = retryFits;
         }

         public string SystemPrompt { get; }
         public IReadOnlyList<LlmMessage> Messages { get; }
         public int MaxTokens { get; }
         public bool RetryFits { get; }
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
         CancellationToken ct = default,
         string? narrativeStyle = null)
      {
         session.AddPlayerMessage(playerMessage);

         string systemPrompt = PromptBuilder.BuildCommonerSystemPrompt(profile, knowledge, narrativeStyle);

         // A commoner's prompt is small and has no compact form: only the conversation is fitted to a declared
         // window (fkasad, 13/10/2026), the oldest lines first.
         IReadOnlyList<LlmMessage> messages = session.Messages;
         LlmParameters parameters = ChatParameters;
         int window = DeclaredWindow();
         if (window > 0)
         {
            ContextFit fit = FitOf(window, systemPrompt, ChatParameters.MaxTokens, session, 1);
            messages = Kept(session, fit);
            Report(profile?.Id, window, fit, switched: false);
            if (ChatParameters.AllowTruncationRetry && !ContextFitPolicy.RetryFits(fit, ChatParameters.MaxTokens))
               parameters = new LlmParameters {
                  MaxTokens = ChatParameters.MaxTokens, Creativity = ChatParameters.Creativity,
                  PresencePenalty = ChatParameters.PresencePenalty, AllowTruncationRetry = false
               };
         }

         var request = new LlmRequest {
            SystemPrompt = systemPrompt,
            Messages = messages,
            Parameters = parameters
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