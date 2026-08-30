// Code written by Gabriel Mailhot, 30/08/2026.
// Extension Surface, Actions guidance: everything an ExternalVerbContract.Guidance callback reasons over.
// Runs PRE-response (before the LLM has produced any prose this turn), so IsEligible here is the CURRENT,
// live eligibility for a modder who wants to phrase different guidance for "not eligible yet" versus
// "eligible, go ahead" (not a record of what the NPC will end up doing, which is only decided once the LLM
// actually answers and, if it emits the [ACTION], Execute runs against a fresh re-check of its own).

using System.Collections.Generic;

namespace NpcMemoryService.Core.Extension
{
   /// <summary>
   ///   The facts an <see cref="ExternalVerbContract.Guidance" /> callback reasons over: who the NPC is,
   ///   what the player just said, whether the action is eligible right now, and which heroes/factions/
   ///   settlements the PLAYER referenced in that message, as the host resolved them.
   ///   <para>
   ///     <see cref="MentionedHeroes" />/<see cref="MentionedFactions" />/<see cref="MentionedSettlements" />
   ///     are what the PLAYER named in their message BEFORE the model has replied, not the action's eventual
   ///     target: the LLM has not yet chosen (or even necessarily will choose) to invoke this action, so
   ///     these lists are context to reason from, never a foregone conclusion about what happens next.
   ///   </para>
   /// </summary>
   public sealed class ActionGuidanceContext
   {
      public ActionGuidanceContext(VerbFacts npc,
                                    string playerMessage,
                                    bool isEligible,
                                    IReadOnlyList<ResolvedEntity> mentionedHeroes,
                                    IReadOnlyList<ResolvedEntity> mentionedFactions,
                                    IReadOnlyList<ResolvedEntity> mentionedSettlements)
      {
         Npc = npc;
         PlayerMessage = playerMessage;
         IsEligible = isEligible;
         MentionedHeroes = mentionedHeroes;
         MentionedFactions = mentionedFactions;
         MentionedSettlements = mentionedSettlements;
      }

      /// <summary>The conversation partner's engine-agnostic facts, the same shape IsEligible/TeachingText reason over.</summary>
      public VerbFacts Npc { get; }

      /// <summary>The player's current message.</summary>
      public string PlayerMessage { get; }

      /// <summary>Whether this action is eligible right now (the same value the verb's own IsEligible callback would return).</summary>
      public bool IsEligible { get; }

      /// <summary>Heroes the player named in their message, as the host resolved them. Never null; empty when none were found.</summary>
      public IReadOnlyList<ResolvedEntity> MentionedHeroes { get; }

      /// <summary>Factions (kingdoms or independent clans) the player named in their message. Never null; empty when none were found.</summary>
      public IReadOnlyList<ResolvedEntity> MentionedFactions { get; }

      /// <summary>Settlements the player named in their message. Never null; empty when none were found.</summary>
      public IReadOnlyList<ResolvedEntity> MentionedSettlements { get; }
   }
}
