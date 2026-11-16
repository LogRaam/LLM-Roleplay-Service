// Code written by Gabriel Mailhot, 16/11/2026.
// The action bench, read by Jev instead of the interpreter.
//
// Dutch (CHIM, Discord 16/11/2026) proposed Jev, TypeSafe AI's decision model, for the second call of the Enhanced
// composition: it answers a typed question (one choice among listed options, with calibrated probabilities) in well under
// a second, for a fraction of a cent, so a prose model weak at actions would be backed by a fast, cheap reader. Gabriel
// (16/11/2026): "prepare le banc Jev". This puts Jev through the SAME ActionBenchCatalog cases the interpreter is scored
// on, so the two read side by side.
//
// What Jev can and cannot be asked (AIML API, POST /v1/decisions): a "choice" picks exactly one key among 1 to 255
// options; it returns no free text, so it names an action TYPE and never its parameters (the fief, the amount, the
// target). The bench therefore asks two choices per case, the main deed and a second, different one (or none), and
// scores TYPES: a case whose types are all right counts as type-right even where the interpreter's scorer would want
// parameters. That is the real question for a hybrid (Jev picks the deed, the interpreter fills its parameters only when
// it needs any), and the report says so. Not shipped: this lives in the live lane only, until numbers decide.

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.LiveLlmTests
{
   /// <summary>One case as Jev read it.</summary>
   public sealed class JevBenchResult
   {
      public ActionBenchCase Case { get; init; } = null!;
      public bool CallSucceeded { get; init; }
      public string? Error { get; init; }
      public string? Main { get; init; }
      public double MainConfidence { get; init; }
      public string? Second { get; init; }
      public ActionBenchVerdict Verdict { get; init; }
      public long Milliseconds { get; init; }
      public double UsdSpent { get; init; }

      /// <summary>Every expected type named (parameters aside), or the forbidden look-alike withheld.</summary>
      public bool TypeRight => CallSucceeded && Verdict is ActionBenchVerdict.Hit or ActionBenchVerdict.ParamMismatch or ActionBenchVerdict.CorrectWithhold;
   }

   /// <summary>Runs ActionBenchCatalog cases through Jev's decision endpoint and scores the action types it picks.</summary>
   public static class JevActionBench
   {
      /// <summary>The option meaning "no deed".</summary>
      public const string NoDeed = "none";

      private const string Endpoint = "https://api.aimlapi.com/v1/decisions";

      /// <summary>
      ///   The options Jev chooses among: every catalog action with its description, every type a case names that the
      ///   catalog lacks (the interpreter's hand-tuned reactive signals), and "none". Jev takes 255 options at most.
      /// </summary>
      public static Dictionary<string, string> Options(IEnumerable<ActionBenchCase> cases)
      {
         var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         foreach (GameActionSpec spec in GameActionCatalog.All)
            if (!string.IsNullOrWhiteSpace(spec.Type) && !options.ContainsKey(spec.Type))
               options[spec.Type] = Shorten(spec.Description, 280);

         foreach (ActionBenchCase c in cases)
         {
            foreach (string type in c.Expected.Select(e => e.Type).Append(c.ForbiddenType ?? ""))
               if (!string.IsNullOrWhiteSpace(type) && !options.ContainsKey(type))
                  options[type] = type.Replace('_', ' ');
         }

         options[NoDeed] = "No deed is carried out in this reply: only talk, a refusal, a condition, or a promise for later.";

         return options.Count <= 255 ? options : options.Take(254).Append(new KeyValuePair<string, string>(NoDeed, options[NoDeed])).ToDictionary(p => p.Key, p => p.Value);
      }

      /// <summary>The request body for one case: the situation and the reply as state, two choice questions.</summary>
      public static string RequestBody(string model, ActionBenchCase test, Dictionary<string, string> options)
      {
         var state = new JsonObject {
            ["situation"] = test.ContextFacts ?? "",
            ["conversation_so_far"] = test.ConversationSoFar ?? "",
            ["npc_reply"] = test.Prose ?? ""
         };

         JsonObject Criteria()
         {
            var o = new JsonObject();
            foreach (KeyValuePair<string, string> p in options) o[p.Key] = p.Value;

            return o;
         }

         var body = new JsonObject {
            ["model"] = model,
            ["state"] = state,
            ["questions"] = new JsonObject {
               ["main"] = new JsonObject {
                  ["type"] = "choice",
                  ["instructions"] = "In the game, which deed does the NPC's reply (npc_reply) actually carry out NOW? Only what is done in this reply: "
                                     + "not something merely narrated from the past, promised for later, offered on a condition, or refused. Choose none if no deed is done.",
                  ["criteria"] = Criteria()
               },
               ["second"] = new JsonObject {
                  ["type"] = "choice",
                  ["instructions"] = "Does the same reply ALSO carry out a second, different deed besides the main one? Choose it, or none if there is no second deed.",
                  ["criteria"] = Criteria()
               }
            }
         };

         return body.ToJsonString();
      }

      /// <summary>Runs the cases through Jev one by one and scores the types it picks.</summary>
      public static async Task<IReadOnlyList<JevBenchResult>> RunAsync(HttpClient http, string apiKey, string model, IReadOnlyList<ActionBenchCase> cases, CancellationToken ct = default)
      {
         Dictionary<string, string> options = Options(cases);
         var results = new List<JevBenchResult>();

         foreach (ActionBenchCase test in cases)
         {
            ct.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();
            try
            {
               using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) {
                  Content = new StringContent(RequestBody(model, test, options), Encoding.UTF8, "application/json")
               };
               request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);

               using HttpResponseMessage response = await http.SendAsync(request, ct).ConfigureAwait(false);
               string text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
               watch.Stop();

               if (!response.IsSuccessStatusCode)
               {
                  results.Add(new JevBenchResult {Case = test, CallSucceeded = false, Error = $"HTTP {(int) response.StatusCode}: {Shorten(text, 200)}", Verdict = ActionBenchVerdict.Miss, Milliseconds = watch.ElapsedMilliseconds});

                  continue;
               }

               JsonNode? root = JsonNode.Parse(text);
               string? main = root?["answers"]?["main"]?["choice"]?.GetValue<string>();
               double confidence = root?["answers"]?["main"]?["confidence"]?.GetValue<double>() ?? 0;
               string? second = root?["answers"]?["second"]?["choice"]?.GetValue<string>();
               double usd = root?["meta"]?["usage"]?["usd_spent"]?.GetValue<double>() ?? 0;

               var emitted = new List<GameAction>();
               if (!string.IsNullOrEmpty(main) && !string.Equals(main, NoDeed, StringComparison.OrdinalIgnoreCase)) emitted.Add(new GameAction {Type = main!});
               if (!string.IsNullOrEmpty(second) && !string.Equals(second, NoDeed, StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(second, main, StringComparison.OrdinalIgnoreCase)) emitted.Add(new GameAction {Type = second!});

               results.Add(new JevBenchResult {
                  Case = test, CallSucceeded = true, Main = main, MainConfidence = confidence, Second = second,
                  Verdict = ActionBenchScorer.Score(emitted, test), Milliseconds = watch.ElapsedMilliseconds, UsdSpent = usd
               });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
               results.Add(new JevBenchResult {Case = test, CallSucceeded = false, Error = ex.GetType().Name + ": " + ex.Message, Verdict = ActionBenchVerdict.Miss, Milliseconds = watch.ElapsedMilliseconds});
            }
         }

         return results;
      }

      /// <summary>The scoreboard: type accuracy on positive cases, withholding on negative ones, latency and cost, then every miss.</summary>
      public static string Format(IReadOnlyList<JevBenchResult> results, string model)
      {
         var sb = new StringBuilder();
         List<JevBenchResult> positives = results.Where(r => !r.Case.IsNegative).ToList();
         List<JevBenchResult> negatives = results.Where(r => r.Case.IsNegative).ToList();
         List<long> times = results.Where(r => r.CallSucceeded).Select(r => r.Milliseconds).OrderBy(t => t).ToList();

         sb.AppendLine($"JEV ACTION BENCH ({model}), {results.Count} cases, types only (Jev names no parameters)");
         sb.AppendLine($"  positive cases, every expected type named: {positives.Count(r => r.TypeRight)}/{positives.Count}");
         sb.AppendLine($"    of which the interpreter's scorer would also pass on parameters: {positives.Count(r => r.Verdict == ActionBenchVerdict.Hit)} (only cases that expect no parameter)");
         sb.AppendLine($"  negative cases, look-alike withheld: {negatives.Count(r => r.TypeRight)}/{negatives.Count}");
         sb.AppendLine($"  call failures: {results.Count(r => !r.CallSucceeded)}");
         if (times.Count > 0)
            sb.AppendLine($"  latency (round trip): median {times[times.Count / 2]} ms, p95 {times[Math.Min(times.Count - 1, (int) (times.Count * 0.95))]} ms, max {times.Last()} ms");
         sb.AppendLine($"  cost reported: ${results.Sum(r => r.UsdSpent):0.000000} total, ${(results.Count == 0 ? 0 : results.Sum(r => r.UsdSpent) / results.Count):0.0000000} per case");
         sb.AppendLine();
         sb.AppendLine("WRONG (case: expected -> Jev main / second, confidence):");
         foreach (JevBenchResult r in results.Where(r => !r.TypeRight))
         {
            string expected = r.Case.IsNegative ? $"not {r.Case.ForbiddenType}" : string.Join(" + ", r.Case.Expected.Select(e => e.Type));
            sb.AppendLine(r.CallSucceeded
               ? $"  {r.Case.Id}: {expected} -> {r.Main} / {r.Second} ({r.MainConfidence:0.00})"
               : $"  {r.Case.Id}: call failed, {r.Error}");
         }

         return sb.ToString();
      }

      private static string Shorten(string? s, int max)
      {
         string t = (s ?? "").Replace('\n', ' ').Trim();

         return t.Length <= max ? t : t.Substring(0, max) + "...";
      }
   }
}
