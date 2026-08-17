// Code written by Gabriel Mailhot, 17/08/2026.
// Runs the interpreter extraction bench for real: each case's static prose is built into the SAME interpreter
// prompt the live chat uses (ActionInterpreterPromptBuilder.Build), sent through the given interpreter client,
// parsed by the SAME SectionResponseParser, and scored by ActionBenchScorer. This is the only piece that spends
// tokens, so it is never called by a unit test; a firewalled live-LLM test and the in-game cr.action_bench drive
// it. The request shape mirrors the mod's ActionInterpreterComposer (system prompt = the built prompt, one nudge
// user message, small max tokens, low temperature); reasoning-off is the caller's client's concern.

#region

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.LlmClient;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Parsing;
using NpcMemoryService.Core.Prompts;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>Runs <see cref="ActionBenchCatalog" /> cases through a real interpreter client into a scored report.</summary>
   public static class ActionBenchRunner
   {
      /// <summary>
      ///   Runs every case in <paramref name="cases" /> through <paramref name="interpreter" /> and scores it,
      ///   <paramref name="passes" /> times each (default 1), so a nondeterministic model's dice can be averaged
      ///   out by the report's majority rule. Cases run sequentially so a caller can watch progress and a
      ///   cancellation stops cleanly. A failed call is recorded as such (never silently scored), so token spend on
      ///   a broken run is visible in the report.
      /// </summary>
      public static async Task<ActionBenchReport> RunAsync(ILlmClient interpreter,
         IReadOnlyList<ActionBenchCase> cases, int passes = 1, CancellationToken ct = default)
      {
         if (passes < 1) passes = 1;

         var parser = new SectionResponseParser();
         var results = new List<ActionBenchResult>();

         for (int pass = 0; pass < passes; pass++)
         foreach (ActionBenchCase test in cases)
         {
            ct.ThrowIfCancellationRequested();

            string prompt = ActionInterpreterPromptBuilder.Build(test.Prose, test.ContextFacts);

            var request = new LlmRequest {
               SystemPrompt = prompt,
               Messages = new[] {new LlmMessage(MessageRole.User, "Output the tags now.")},
               Parameters = new LlmParameters {MaxTokens = 600, Creativity = 0.2f}
            };

            LlmResponse response = await interpreter.CompleteAsync(request, ct).ConfigureAwait(false);

            if (response == null || !response.IsSuccess)
            {
               results.Add(new ActionBenchResult(test, false, response?.ErrorMessage ?? "no result",
                  null, ActionBenchVerdict.Miss));

               continue;
            }

            IReadOnlyList<GameAction> emitted = parser.Parse(response.Content).Actions;
            ActionBenchVerdict verdict = ActionBenchScorer.Score(emitted, test);

            results.Add(new ActionBenchResult(test, true, null, emitted, verdict));
         }

         return new ActionBenchReport(results);
      }
   }
}
