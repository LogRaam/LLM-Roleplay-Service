// Code written by Gabriel Mailhot, 15/09/2026.
// The measurement a player had to do by hand.
//
// fkasad (Nexus, 15/09/2026) found the prompt cache dying on turn two by saving two successive prompts
// and diffing them: "the second package kills the cache very early, after line 29 (98% of the cache just
// died)". That was real work, done from the outside, on a defect nothing in the code was watching for.
//
// This is that diff as a function. Two cacheable prefixes in, one honest number out: how much of the
// first the second can still read. It is what an ILlmWireObserver reports per request, so the next time
// a per-turn fact drifts above the marker, the line says so instead of waiting for someone to notice a
// bill.
//
// It measures CHARACTERS, not tokens, and deliberately: the client has the text and not the tokeniser,
// and the ratio is what matters. A provider's own usage numbers (prompt/cached) are the ground truth and
// are logged separately by the host - these two disagreeing is itself worth knowing, since it means the
// provider is not honouring the breakpoint.

#region

using System;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   How much of a cacheable prefix survives from one request to the next. Pure: no clock, no I/O, no
   ///   provider.
   /// </summary>
   public static class PromptCacheReport
   {
      /// <summary>
      ///   Compares this request's cacheable prefix with the previous one's. A <see cref="Verdict" /> of
      ///   <see cref="PrefixVerdict.NoPrevious" /> is the first request of a session and means nothing is
      ///   wrong; <see cref="PrefixVerdict.Whole" /> is the healthy steady state.
      /// </summary>
      public static PrefixComparison Compare(string? previousPrefix, string? currentPrefix)
      {
         if (string.IsNullOrEmpty(currentPrefix))
            return new PrefixComparison {Verdict = PrefixVerdict.NotSplit};

         if (previousPrefix == null)
            return new PrefixComparison {
               Verdict = PrefixVerdict.NoPrevious, PrefixChars = currentPrefix!.Length
            };

         int shared = SharedPrefixLength(previousPrefix, currentPrefix!);
         bool whole = shared == previousPrefix.Length && shared == currentPrefix!.Length;

         return new PrefixComparison {
            Verdict = whole ? PrefixVerdict.Whole : PrefixVerdict.Broken,
            PrefixChars = currentPrefix!.Length,
            SharedChars = shared,
            DivergesAt = whole ? -1 : shared
         };
      }

      private static int SharedPrefixLength(string a, string b)
      {
         int n = Math.Min(a.Length, b.Length);

         for (var i = 0; i < n; i++)
            if (a[i] != b[i])
               return i;

         return n;
      }
   }

   /// <summary>What happened to the cacheable prefix between two requests.</summary>
   public enum PrefixVerdict
   {
      /// <summary>No marker in the prompt, so there is no split and the client caches it whole.</summary>
      NotSplit,

      /// <summary>The first request seen. Its prefix is being written, not read.</summary>
      NoPrevious,

      /// <summary>Byte-for-byte identical: the whole prefix can be read from cache. The healthy case.</summary>
      Whole,

      /// <summary>The prefix moved, so everything from the divergence on must be paid for again.</summary>
      Broken
   }

   /// <summary>One request's cache verdict, in the terms a bill is paid in.</summary>
   public sealed record PrefixComparison
   {
      public PrefixVerdict Verdict { get; init; }

      /// <summary>Characters in this request's cacheable prefix.</summary>
      public int PrefixChars { get; init; }

      /// <summary>Characters this request shares with the previous one, from the start.</summary>
      public int SharedChars { get; init; }

      /// <summary>Where the two prefixes first differ, or -1 when they do not.</summary>
      public int DivergesAt { get; init; } = -1;

      /// <summary>
      ///   The share of this prefix that survived, 0 to 1. A first request reports 0 because nothing was
      ///   reused, which is true and not a fault - read it with <see cref="Verdict" />, never alone.
      /// </summary>
      public double Reuse => PrefixChars <= 0 ? 0 : (double) SharedChars / PrefixChars;

      /// <summary>One line for a log, saying what it costs rather than what it is.</summary>
      public override string ToString()
         => Verdict switch {
            PrefixVerdict.NotSplit => "prefix: none (no encounter marker — the whole prompt is cached, so every turn will bust it)",
            PrefixVerdict.NoPrevious => $"prefix: {PrefixChars} chars, first request (written, not read)",
            PrefixVerdict.Whole => $"prefix: {PrefixChars} chars, fully reused",
            _ => $"prefix: {PrefixChars} chars, diverges at {DivergesAt} — {(1 - Reuse) * 100:F1}% discarded"
         };
   }
}
