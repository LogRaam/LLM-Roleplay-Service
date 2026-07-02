using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NpcMemoryService.Core.Compression;
using NpcMemoryService.Core.LlmClient.OpenRouter;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;
using NpcMemoryService.Core.Services;
using NpcMemoryService.Core.Storage;

namespace NpcMemoryService.ConsoleRunner
{
    /// <summary>
    /// Reference harness for the NpcMemoryService SDK.
    /// Modders can read Main() to see the full wiring of a chat session.
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            RunnerSession s = await BuildSession();
            PrintWelcome(s);

            while (true)
            {
                Console.Write("\nYou: ");
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                bool keepRunning = await DispatchCommand(input, s);
                if (!keepRunning) break;
            }
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        private static async Task<RunnerSession> BuildSession()
        {
            var llmConfig = new OpenRouterConfig
            {
                ApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                         ?? throw new InvalidOperationException(
                             "Set the OPENROUTER_API_KEY environment variable."),
                Model  = "x-ai/grok-4.20"
            };

            var storeConfig = new JsonFileStoreConfig
            {
                Directory = Path.Combine(AppContext.BaseDirectory, "npc-data")
            };

            var httpClient    = new HttpClient();
            var llmClient     = new OpenRouterClient(httpClient, llmConfig);
            var parser        = new SectionResponseParser();
            var promptBuilder = new PromptBuilder
            {
                ActionVocabulary = BuildExampleActionVocabulary()
            };
            var chatService   = new NpcChatService(llmClient, parser, promptBuilder);
            var store         = new JsonFileNpcMemoryStore(storeConfig);
            var compressor    = new LlmMemoryCompressor(llmClient);

            await store.InitializeAsync();

            NpcProfile npc = store.Get("npc_raganvad") ?? CreateDefaultNpc();

            return new RunnerSession
            {
                HttpClient     = httpClient,
                ChatService    = chatService,
                Store          = store,
                Compressor     = compressor,
                Npc            = npc,
                World          = BuildWorldState(),
                ChatSession    = new ChatSession(),
                StoreDirectory = storeConfig.Directory
            };
        }

        private static NpcProfile CreateDefaultNpc() => new NpcProfile
        {
            Id          = "npc_raganvad",
            Name        = "Raganvad",
            Faction     = "Sturgia",
            Clan        = "Vagiroving",
            Personality = "Proud and honorable Sturgian king. Values loyalty above all. " +
                          "Speaks bluntly and dislikes flattery. Slow to trust but fiercely " +
                          "loyal to those who earn it."
        };

        private static WorldState BuildWorldState() => new WorldState
        {
            CurrentDay      = 142,
            ActiveConflicts = "Sturgia is at war with the Western Empire.",
            Rumors          = "The player recently sacked a Vlandian castle."
        };

        /// <summary>
        /// A starter Bannerlord-flavored action vocabulary. In the real mod,
        /// these would map to TaleWorlds API calls via an IGameActionHandler.
        /// </summary>
        private static IReadOnlyList<GameActionDefinition> BuildExampleActionVocabulary() =>
            new List<GameActionDefinition>
            {
                new() { Type = "imprison",
                        Description = "Take the player as prisoner." },
                new() { Type = "release_prisoner",
                        Description = "Release the player from captivity." },
                new() { Type = "give_money",
                        Description = "Transfer denars from NPC to player.",
                        Parameters = new[] { "amount" } },
                new() { Type = "take_money",
                        Description = "Demand denars from the player.",
                        Parameters = new[] { "amount" } },
                new() { Type = "give_item",
                        Description = "Give an item to the player.",
                        Parameters = new[] { "item", "quantity" } },
                new() { Type = "recruit_player",
                        Description = "Invite the player to join the NPC's party or clan." },
                new() { Type = "initiate_combat",
                        Description = "Begin hostile combat with the player." },
                new() { Type = "confiscate_weapons",
                        Description = "Strip the player of their weapons." },
                new() { Type = "schedule_meeting",
                        Description = "Demand the player return on a specific future day.",
                        Parameters = new[] { "day" } }
            };

        // ── Command dispatch ──────────────────────────────────────────────────

        /// <summary>Returns false when the loop should exit.</summary>
        private static async Task<bool> DispatchCommand(string input, RunnerSession s)
        {
            // Time-advance command: "day +N" or "day N"
            if (input.StartsWith("day ", StringComparison.OrdinalIgnoreCase))
            {
                AdvanceDay(input.Substring(4).Trim(), s);
                return true;
            }

            switch (input.ToLowerInvariant())
            {
                case "quit":
                    Console.WriteLine("[Session ended — unsaved changes discarded]");
                    return false;

                case "save":
                    await s.Store.CommitAsync();
                    Console.WriteLine($"[Saved to {s.StoreDirectory}]");
                    return true;

                case "debug":
                    s.ShowDebug = !s.ShowDebug;
                    Console.WriteLine($"[Debug output {(s.ShowDebug ? "ON" : "OFF")}]");
                    return true;

                case "memory":
                    PrintMemoryDigest(s.Npc);
                    return true;

                case "events":
                    PrintEvents(s.Npc);
                    return true;

                case "compress":
                    await RunCompression(s.Npc, s.Compressor);
                    return true;

                default:
                    await RunChatTurn(input, s);
                    return true;
            }
        }

        private static void AdvanceDay(string arg, RunnerSession s)
        {
            int delta;
            if (arg.StartsWith("+", StringComparison.Ordinal) &&
                int.TryParse(arg.Substring(1), out int parsedDelta))
            {
                delta = parsedDelta;
            }
            else if (int.TryParse(arg, out int parsedAbsolute) && parsedAbsolute > s.World.CurrentDay)
            {
                delta = parsedAbsolute - s.World.CurrentDay;
            }
            else
            {
                Console.WriteLine("[Usage: 'day +N' to advance by N days, or 'day N' for absolute day]");
                return;
            }

            s.World = new WorldState
            {
                CurrentDay      = s.World.CurrentDay + delta,
                ActiveConflicts = s.World.ActiveConflicts,
                Rumors          = s.World.Rumors
            };

            Console.WriteLine($"[Advanced to day {s.World.CurrentDay} (+{delta})]");
        }

        // ── Chat turn ─────────────────────────────────────────────────────────

        private static async Task RunChatTurn(string playerMessage, RunnerSession s)
        {
            Console.WriteLine("\nThinking...");
            NpcChatResult result = await s.ChatService.ChatAsync(s.Npc, s.World, s.ChatSession, playerMessage);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"[ERROR] {result.ErrorMessage}");
                return;
            }

            ParsedResponse response = result.Response!;
            Console.WriteLine($"\n{s.Npc.Name}: {response.Dialogue}");

            // Apply to in-memory state only — no I/O until 'save'.
            // ProfileMutator.Apply is the single authoritative mutation path (shared with the
            // mod), so this runner exercises the same behaviour testers see in-game.
            ProfileMutator.Apply(s.Npc, response, s.World.CurrentDay);
            s.Store.Set(s.Npc);

            if (s.ShowDebug)
                PrintDebugSections(response, result.Usage, s.Npc);

            // End-of-encounter detection: clear the chat history so the next
            // interaction starts a fresh session. Long-term memory persists
            // through the NpcProfile; only the per-encounter dialogue resets.
            if (response.NewEventData?.Type == NotableEventType.Farewell)
            {
                s.ChatSession = new ChatSession();
                Console.WriteLine("\n[Encounter ended — chat history cleared. NPC memory persists.]");
                Console.WriteLine("[Use 'day +N' to advance time before the next encounter.]");
            }
        }

        // ── Output helpers ────────────────────────────────────────────────────

        private static void PrintWelcome(RunnerSession s)
        {
            Console.WriteLine("=== NPC Memory Service — Console Runner ===");
            Console.WriteLine($"NPC: {s.Npc.Name} ({s.Npc.Clan}, {s.Npc.Faction})");
            Console.WriteLine($"Day: {s.World.CurrentDay}  |  Reputation: {s.Npc.ReputationWithPlayer}");

            if (!string.IsNullOrWhiteSpace(s.Npc.MemoryDigest))
            {
                Console.WriteLine("\n[Memory loaded from previous session]");
                Console.WriteLine(s.Npc.MemoryDigest);
            }

            Console.WriteLine("\nCommands: 'save' — persist  |  'memory' — show digest");
            Console.WriteLine("          'compress' — compress old events  |  'events' — list events");
            Console.WriteLine("          'day +N' — advance time  |  'debug' — toggle sections");
            Console.WriteLine("          'quit' — exit without saving");
            Console.WriteLine(new string('─', 60));
        }

        private static void PrintMemoryDigest(NpcProfile npc)
        {
            Console.WriteLine(string.IsNullOrWhiteSpace(npc.MemoryDigest)
                ? "[No memory yet]"
                : npc.MemoryDigest);
        }

        private static void PrintEvents(NpcProfile npc)
        {
            if (npc.Events.Count == 0)
            {
                Console.WriteLine("[No events yet]");
                return;
            }

            Console.WriteLine($"[{npc.Events.Count} event(s)]");
            for (int i = 0; i < npc.Events.Count; i++)
            {
                var ev = npc.Events[i];
                Console.WriteLine($"  [{i}] Day {ev.gameDay} ({ev.type}): {ev.summary}");
            }

            if (!string.IsNullOrWhiteSpace(npc.BackgroundContext))
                Console.WriteLine($"\n  Background: {npc.BackgroundContext}");
        }

        private static void PrintDebugSections(ParsedResponse response, LlmUsage? usage, NpcProfile npc)
        {
            Console.WriteLine(new string('─', 60));

            if (response.Memory != null)
                Console.WriteLine($"[MEMORY]  topic={response.Memory.Topic} | " +
                                  $"sentiment={response.Memory.Sentiment} | " +
                                  $"decision={response.Memory.Decision ?? "—"}");

            if (response.NewEventData != null)
                Console.WriteLine($"[EVENT]   type={response.NewEventData.Type} | " +
                                  $"summary={response.NewEventData.Summary}");

            if (response.Reputation != null)
                Console.WriteLine($"[REP]     clan_delta={response.Reputation.ClanDelta} | " +
                                  $"new_score={npc.ReputationWithPlayer}");

            foreach (GameAction action in response.Actions)
            {
                string paramStr = action.Parameters.Count == 0
                    ? string.Empty
                    : " | " + string.Join(", ",
                        action.Parameters.Select(p => $"{p.Key}={p.Value}"));
                string contextStr = string.IsNullOrEmpty(action.Context)
                    ? string.Empty
                    : $" ({action.Context})";
                Console.WriteLine($"[ACTION]  type={action.Type}{paramStr}{contextStr}");
            }

            if (usage != null)
                Console.WriteLine($"[TOKENS]  prompt={usage.PromptTokens} " +
                                  $"(cached:{usage.CachedPromptTokens ?? 0}) | " +
                                  $"completion={usage.CompletionTokens}");

            Console.WriteLine(new string('─', 60));
        }

        // ── Compression ───────────────────────────────────────────────────────

        private static async Task RunCompression(NpcProfile npc, IMemoryCompressor compressor)
        {
            int before = npc.Events.Count;

            if (before < 12)
            {
                Console.WriteLine($"[Only {before} event(s) — below threshold, nothing to compress]");
                return;
            }

            Console.WriteLine($"[Compressing {before} events...]");
            CompressionResult result = await compressor.CompressAsync(npc);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"[Compression failed: {result.ErrorMessage}]");
                return;
            }

            if (result.DroppedCount == 0)
            {
                Console.WriteLine("[No events were dropped — nothing to compress]");
                return;
            }

            ApplyCompression(npc, result);

            Console.WriteLine($"[Compressed: {before} → {npc.Events.Count} events ({result.DroppedCount} dropped)]");

            if (!string.IsNullOrWhiteSpace(result.BackgroundSummary))
                Console.WriteLine($"[Background summary: {result.BackgroundSummary}]");

            if (result.Usage != null)
                Console.WriteLine($"[Compression tokens: prompt={result.Usage.PromptTokens} | completion={result.Usage.CompletionTokens}]");

            Console.WriteLine("[Changes are in memory only — use 'save' to persist]");
        }

        private static void ApplyCompression(NpcProfile npc, CompressionResult result)
        {
            npc.Events.Clear();
            foreach (NotableEvent ev in result.KeptEvents)
                npc.Events.Add(ev);

            if (!string.IsNullOrWhiteSpace(result.BackgroundSummary))
            {
                npc.BackgroundContext = string.IsNullOrWhiteSpace(npc.BackgroundContext)
                    ? result.BackgroundSummary
                    : npc.BackgroundContext + " " + result.BackgroundSummary;
            }
        }
    }

    /// <summary>
    /// Holds the mutable and immutable state of the interactive runner session.
    /// </summary>
    internal sealed class RunnerSession
    {
        public required HttpClient        HttpClient     { get; init; }
        public required NpcChatService    ChatService    { get; init; }
        public required INpcMemoryStore   Store          { get; init; }
        public required IMemoryCompressor Compressor     { get; init; }
        public required NpcProfile        Npc            { get; init; }
        public required string            StoreDirectory { get; init; }
        public          WorldState        World          { get; set; } = null!;
        public          ChatSession       ChatSession    { get; set; } = null!;
        public          bool              ShowDebug      { get; set; }
    }
}
