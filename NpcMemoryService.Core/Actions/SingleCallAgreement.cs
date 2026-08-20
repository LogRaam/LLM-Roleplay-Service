// Code written by Gabriel Mailhot, 19/08/2026.
// The scoring heart of the single-call bench: does the single-call model's OWN tags match what a dedicated interpreter
// extracts from the SAME authored prose? Agreement means the single call self-extracts as well as the proven two-call
// pipeline (so the second call can be retired); divergence pinpoints where self-grounding drifts from an independent
// read - the theoretical risk of one model both writing and judging its own reply. The comparison is on SUBSTANTIVE
// action types: change_relation and end_conversation are noisy chat-flow fallbacks (either side may add a small one),
// so they never decide the verdict, though they are still reported for the reader.

#region

using System.Collections.Generic;
using System.Linq;

#endregion

namespace NpcMemoryService.Core.Actions
{
   /// <summary>Agreement between the single call's self-tags and the interpreter's tags on the same prose.</summary>
   public enum SingleCallAgreementVerdict
   {
      /// <summary>Both sides emitted the same set of substantive action types.</summary>
      Agree,

      /// <summary>The substantive action types differ (see the only-self / only-interpreter lists).</summary>
      Diverge
   }

   /// <summary>The result of comparing one single-call reply's self-tags against the interpreter's tags.</summary>
   public sealed class SingleCallAgreementResult
   {
      /// <summary>Creates a result. The lists name substantive types present on only one side.</summary>
      public SingleCallAgreementResult(SingleCallAgreementVerdict verdict,
         IReadOnlyList<string> onlySelf, IReadOnlyList<string> onlyInterpreter)
      {
         Verdict = verdict;
         OnlySelf = onlySelf;
         OnlyInterpreter = onlyInterpreter;
      }

      /// <summary>Agree when the substantive type sets match, Diverge otherwise.</summary>
      public SingleCallAgreementVerdict Verdict { get; }

      /// <summary>Substantive types the single call emitted that the interpreter did not.</summary>
      public IReadOnlyList<string> OnlySelf { get; }

      /// <summary>Substantive types the interpreter emitted that the single call did not.</summary>
      public IReadOnlyList<string> OnlyInterpreter { get; }
   }

   /// <summary>Compares two tag sets by substantive action type.</summary>
   public static class SingleCallAgreement
   {
      // change_relation and end_conversation are the general chat-flow fallbacks: either the composer or the
      // interpreter may add a small one without it being a real disagreement about what DEED happened, so they are
      // excluded from the verdict (still surfaced elsewhere for the reader).
      private static readonly HashSet<string> NonSubstantive = new HashSet<string> {
         "change_relation", "end_conversation"
      };

      /// <summary>
      ///   Compares the single call's <paramref name="selfTypes" /> against the interpreter's
      ///   <paramref name="interpreterTypes" />. Null is treated as an empty set (e.g. a failed interpreter call is
      ///   scored as "extracted nothing", never silently as agreement). The verdict is Agree only when the two
      ///   SUBSTANTIVE type sets are identical.
      /// </summary>
      public static SingleCallAgreementResult Compare(IEnumerable<string> selfTypes, IEnumerable<string> interpreterTypes)
      {
         HashSet<string> self = Substantive(selfTypes);
         HashSet<string> interp = Substantive(interpreterTypes);

         List<string> onlySelf = self.Where(t => !interp.Contains(t)).OrderBy(t => t).ToList();
         List<string> onlyInterp = interp.Where(t => !self.Contains(t)).OrderBy(t => t).ToList();

         SingleCallAgreementVerdict verdict = onlySelf.Count == 0 && onlyInterp.Count == 0
            ? SingleCallAgreementVerdict.Agree
            : SingleCallAgreementVerdict.Diverge;

         return new SingleCallAgreementResult(verdict, onlySelf, onlyInterp);
      }

      private static HashSet<string> Substantive(IEnumerable<string> types)
      {
         var set = new HashSet<string>();
         if (types == null) return set;

         foreach (string t in types)
            if (!string.IsNullOrWhiteSpace(t) && !NonSubstantive.Contains(t))
               set.Add(t);

         return set;
      }
   }
}
