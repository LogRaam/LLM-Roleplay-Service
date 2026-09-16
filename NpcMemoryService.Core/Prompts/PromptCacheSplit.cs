// Code written by Gabriel Mailhot, 15/09/2026.
// Where the prompt cache breaks, expressed once so it can be tested.
//
// The rule itself is old: everything up to the CURRENT ENCOUNTER marker is stable for the whole
// conversation and carries the provider's cache breakpoint; everything after it is per-turn and is
// sent fresh. It lived as an inline IndexOf inside NpcChatService, which meant a test could only
// MIRROR it - and a mirror of the rule cannot catch the rule being broken.
//
// It was broken. A player (fkasad, Nexus, 15/09/2026) diffed two successive prompts from one
// conversation and found the cacheable prefix diverging 2.5% in, because a per-turn fact
// (IsConversationOpening) was being rendered up in the format teaching. Every second turn of every
// conversation was therefore paying a full prefix write instead of a cache read.
//
// So the rule moves here, production calls it, and PromptCacheStabilityTests asserts against the
// same function the game runs.

#region

using System;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   The cacheable/volatile boundary of a system prompt. One rule, one place: the game and the
   ///   tests that guard it read the same function.
   /// </summary>
   public static class PromptCacheSplit
   {
      /// <summary>
      ///   The part of <paramref name="systemPrompt" /> that carries the cache breakpoint: everything
      ///   before <see cref="PromptBuilder.EncounterSectionHeading" />. Null when the marker is absent
      ///   or opens the prompt, which is the client's signal to cache the prompt whole (prior
      ///   behaviour) rather than guess at a split.
      /// </summary>
      public static string? StablePrefixOf(string? systemPrompt)
      {
         if (string.IsNullOrEmpty(systemPrompt)) return null;

         int splitAt = systemPrompt!.IndexOf(PromptBuilder.EncounterSectionHeading, StringComparison.Ordinal);

         return splitAt > 0
            ? systemPrompt.Substring(0, splitAt)
            : null;
      }
   }
}
