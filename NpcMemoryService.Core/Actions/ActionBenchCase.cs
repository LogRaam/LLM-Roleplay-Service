// Code written by Gabriel Mailhot, 17/08/2026.
// Interpreter extraction bench: measures whether the Action Interpreter, given a hand-written NPC reply, emits the
// RIGHT action (and withholds the wrong one). The prose is authored by hand so the ONLY variable is the
// interpreter itself, which isolates its extraction fidelity from any prose-model variance. This file is the pure
// data shape of one case; ActionBenchScorer judges an interpreter's emitted actions against it, and
// ActionBenchCatalog holds the cases. No engine, no LLM here.

#region

using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>How one bench case scored, once the interpreter has run and its actions are compared to the case.</summary>
   public enum ActionBenchVerdict
   {
      /// <summary>Positive case: the expected action was emitted with every required parameter matching.</summary>
      Hit,

      /// <summary>Positive case: the expected action was never emitted.</summary>
      Miss,

      /// <summary>Positive case: the action was emitted, but a required parameter was wrong or absent.</summary>
      ParamMismatch,

      /// <summary>Negative case: the forbidden look-alike action was correctly NOT emitted.</summary>
      CorrectWithhold,

      /// <summary>Negative case: the forbidden look-alike action was wrongly emitted.</summary>
      FalsePositive
   }

   /// <summary>
   ///   One extraction case: a static, hand-written NPC reply (<see cref="Prose" />) plus the minimal
   ///   <see cref="ContextFacts" /> digest the interpreter needs, paired with what it SHOULD or must NOT extract.
   ///   A POSITIVE case (<see cref="ExpectedType" /> set) asserts the deed is recognised with the right parameters.
   ///   A NEGATIVE case (<see cref="ForbiddenType" /> set) asserts a look-alike is withheld, the three families the
   ///   catalog's anti-patterns name: narrated-but-not-done, a future or conditional promise, and a neighbouring
   ///   verb. <see cref="Verb" /> names the catalog verb this case exercises, so coverage (one case per verb) can be
   ///   asserted purely.
   /// </summary>
   public sealed class ActionBenchCase
   {
      private static readonly IReadOnlyDictionary<string, string> NoParams = new Dictionary<string, string>();

      private ActionBenchCase(string id, string verb, string contextFacts, string prose, string expectedType,
         IReadOnlyDictionary<string, string> expectedParams, string forbiddenType)
      {
         Id = id;
         Verb = verb;
         ContextFacts = contextFacts;
         Prose = prose;
         ExpectedType = expectedType;
         ExpectedParams = expectedParams ?? NoParams;
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

      /// <summary>The action the interpreter SHOULD emit, or null on a negative case.</summary>
      public string ExpectedType { get; }

      /// <summary>The parameters the expected action must carry (subset match); empty when the verb takes none.</summary>
      public IReadOnlyDictionary<string, string> ExpectedParams { get; }

      /// <summary>The look-alike action the interpreter must NOT emit, or null on a positive case.</summary>
      public string ForbiddenType { get; }

      /// <summary>True when this is a negative case (a withholding test), false when it expects an emission.</summary>
      public bool IsNegative => ExpectedType == null;

      /// <summary>A positive case: the interpreter should emit <paramref name="expectedType" /> with the given params.</summary>
      public static ActionBenchCase Expect(string id, string verb, string contextFacts, string prose,
         string expectedType, IReadOnlyDictionary<string, string> expectedParams = null)
         => new ActionBenchCase(id, verb, contextFacts, prose, expectedType, expectedParams, null);

      /// <summary>A negative case: the interpreter must NOT emit <paramref name="forbiddenType" /> for this prose.</summary>
      public static ActionBenchCase ExpectNone(string id, string verb, string contextFacts, string prose,
         string forbiddenType)
         => new ActionBenchCase(id, verb, contextFacts, prose, null, NoParams, forbiddenType);
   }
}
