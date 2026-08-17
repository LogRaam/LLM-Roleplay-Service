// Code written by Gabriel Mailhot, 17/08/2026.
// Interpreter extraction bench: measures whether the Action Interpreter, given a hand-written NPC reply, emits the
// RIGHT action(s) (and withholds the wrong one). The prose is authored by hand so the ONLY variable is the
// interpreter itself, which isolates its extraction fidelity from any prose-model variance. This file is the pure
// data shape of one case; ActionBenchScorer judges an interpreter's emitted actions against it, and
// ActionBenchCatalog holds the cases. No engine, no LLM here.
// A case may expect MORE THAN ONE action (a reply that does two concrete deeds), so a "1 of 2" extraction, the
// interpreter emitting only one, is caught rather than passing as a hit.

#region

using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>How one bench case scored, once the interpreter has run and its actions are compared to the case.</summary>
   public enum ActionBenchVerdict
   {
      /// <summary>Positive case: EVERY expected action was emitted with its required parameters.</summary>
      Hit,

      /// <summary>Positive case: some but not all expected actions were emitted (a "1 of 2" extraction).</summary>
      PartialHit,

      /// <summary>Positive case: none of the expected actions were emitted at all.</summary>
      Miss,

      /// <summary>Positive case: every expected type was emitted, but a required parameter was wrong or absent.</summary>
      ParamMismatch,

      /// <summary>Negative case: the forbidden look-alike action was correctly NOT emitted.</summary>
      CorrectWithhold,

      /// <summary>Negative case: the forbidden look-alike action was wrongly emitted.</summary>
      FalsePositive
   }

   /// <summary>One action a positive case expects: a type and the parameters that action must carry (subset match).</summary>
   public sealed class ExpectedAction
   {
      public ExpectedAction(string type, IReadOnlyDictionary<string, string> parameters)
      {
         Type = type;
         Params = parameters ?? new Dictionary<string, string>();
      }

      public string Type { get; }
      public IReadOnlyDictionary<string, string> Params { get; }
   }

   /// <summary>
   ///   One extraction case: a static, hand-written NPC reply (<see cref="Prose" />) plus the minimal
   ///   <see cref="ContextFacts" /> digest the interpreter needs, paired with what it SHOULD or must NOT extract.
   ///   A POSITIVE case (<see cref="Expected" /> non-empty) asserts each expected action is recognised with the
   ///   right parameters; two expected actions catch a reply whose second deed the interpreter drops. A NEGATIVE
   ///   case (<see cref="ForbiddenType" /> set) asserts a look-alike is withheld: narrated-but-not-done, a future
   ///   or conditional promise, or a neighbouring verb. <see cref="Verb" /> names the catalog verb this case
   ///   exercises, so coverage (one case per verb) can be asserted purely.
   /// </summary>
   public sealed class ActionBenchCase
   {
      private static readonly IReadOnlyDictionary<string, string> NoParams = new Dictionary<string, string>();

      private ActionBenchCase(string id, string verb, string contextFacts, string prose,
         IReadOnlyList<ExpectedAction> expected, string forbiddenType)
      {
         Id = id;
         Verb = verb;
         ContextFacts = contextFacts;
         Prose = prose;
         Expected = expected ?? new List<ExpectedAction>();
         ForbiddenType = forbiddenType;
      }

      /// <summary>A short, unique label for the case (usually the verb, or the verb plus a variant suffix).</summary>
      public string Id { get; }

      /// <summary>The catalog verb this case exercises, the key coverage is asserted on (every verb needs a case).</summary>
      public string Verb { get; }

      /// <summary>The situational digest handed to the interpreter alongside the prose (kept minimal per case).</summary>
      public string ContextFacts { get; }

      /// <summary>The static, hand-written NPC reply the interpreter reads. The only variable in the whole bench.</summary>
      public string Prose { get; }

      /// <summary>The action(s) the interpreter SHOULD emit (one or more), or empty on a negative case.</summary>
      public IReadOnlyList<ExpectedAction> Expected { get; }

      /// <summary>The look-alike action the interpreter must NOT emit, or null on a positive case.</summary>
      public string ForbiddenType { get; }

      /// <summary>True when this is a negative case (a withholding test), false when it expects an emission.</summary>
      public bool IsNegative => Expected.Count == 0;

      /// <summary>A positive case: the interpreter should emit <paramref name="expectedType" /> with the given params.</summary>
      public static ActionBenchCase Expect(string id, string verb, string contextFacts, string prose,
         string expectedType, IReadOnlyDictionary<string, string> expectedParams = null)
         => new ActionBenchCase(id, verb, contextFacts, prose,
            new List<ExpectedAction> {new ExpectedAction(expectedType, expectedParams)}, null);

      /// <summary>Adds another expected action to a positive case, so the reply must produce BOTH (a multi-action case).</summary>
      public ActionBenchCase And(string expectedType, IReadOnlyDictionary<string, string> expectedParams = null)
         => new ActionBenchCase(Id, Verb, ContextFacts, Prose,
            Expected.Concat(new[] {new ExpectedAction(expectedType, expectedParams)}).ToList(), ForbiddenType);

      /// <summary>A negative case: the interpreter must NOT emit <paramref name="forbiddenType" /> for this prose.</summary>
      public static ActionBenchCase ExpectNone(string id, string verb, string contextFacts, string prose,
         string forbiddenType)
         => new ActionBenchCase(id, verb, contextFacts, prose, new List<ExpectedAction>(), forbiddenType);
   }
}
