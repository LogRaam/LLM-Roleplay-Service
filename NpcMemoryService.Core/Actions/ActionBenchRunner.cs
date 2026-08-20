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

            string contextFacts = ActionInterpreterContextBuilder.AppendConversationSoFar(test.ContextFacts, test.ConversationSoFar);
            string prompt = ActionInterpreterPromptBuilder.Build(test.Prose, contextFacts);

            var request = new LlmRequest {
               SystemPrompt = prompt,
               Messages = new[] {new LlmMessage(MessageRole.User, ActionInterpreterPromptBuilder.FinalInstruction)},
               // 900, not 600: the two-step FinalInstruction makes the model write a CHECK grounding line per
               // candidate action BEFORE the tag block, so a reply carrying several deeds can exhaust 600 on the
               // CHECK preamble and truncate the tags (bench 2026-08-19 16:06: empty emits + a "no content" call).
               Parameters = new LlmParameters {MaxTokens = 900, Creativity = 0.2f}
            };

            LlmResponse response = await interpreter.CompleteAsync(request, ct).ConfigureAwait(false);

            if (response == null || !response.IsSuccess)
            {
               results.Add(new ActionBenchResult(test, false, response?.ErrorMessage ?? "no result",
                  null, ActionBenchVerdict.Miss));

               continue;
            }

            ParsedResponse parsed = parser.Parse(response.Content);
            IReadOnlyList<GameAction> emitted = parsed.Actions;
            ActionBenchVerdict verdict = ActionBenchScorer.Score(emitted, test);

            string emittedEvent = parsed.NewEventData == null
               ? null
               : parsed.NewEventData.Type + Summarize(parsed.NewEventData.Summary);

            results.Add(new ActionBenchResult(test, true, null, emitted, verdict, emittedEvent));
         }

         return new ActionBenchReport(results);
      }

      // A short, one-line tail for the logged EVENT summary so a report line stays readable.
      private static string Summarize(string summary)
      {
         if (string.IsNullOrWhiteSpace(summary)) return string.Empty;

         string s = summary.Trim().Replace('\n', ' ');

         return ": " + (s.Length <= 60 ? s : s.Substring(0, 60) + "...");
      }
   }
}
