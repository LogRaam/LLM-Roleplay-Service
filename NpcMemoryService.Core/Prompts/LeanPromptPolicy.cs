// Code written by Gabriel Mailhot, 01/07/2026.

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>How aggressively to trim the system prompt so it fits a small model's short context window.</summary>
   public enum LeanPromptLevel
   {
      /// <summary>Everything — the full prompt (default; for capable / large-context models).</summary>
      Full,

      /// <summary>Compact — drop the heavy flavour/context sections and shorten the rest, to fit short (~4k–8k) contexts.</summary>
      Lean
   }

   /// <summary>The heavy, always-on system-prompt sections a lean prompt may drop to save tokens.</summary>
   public enum PromptSection
   {
      /// <summary>The top-friends / top-enemies / family / liege relationship lists.</summary>
      Relationships,

      /// <summary>The world narrative (world.txt) and player description flavour.</summary>
      WorldNarrative,

      /// <summary>The NPC's cultural traditions / background context.</summary>
      CulturalBackground,

      /// <summary>The long player-authored backstory.</summary>
      AuthoredBackstory,

      /// <summary>The full (file-authored) behaviour guidelines block; Lean substitutes a short built-in set.</summary>
      FullBehaviorGuidelines,

      /// <summary>
      ///   The witness reaction machinery (PROVOKED/PROACTIVE, [WITNESS_REACTION] formatting rules).
      ///   Lean keeps only the bare witness list, when present, so candor still adjusts.
      /// </summary>
      WitnessMachineryTeaching,

      /// <summary>The full anti-cliche blocklist in PROSE CRAFT. Lean keeps one line: be specific, never break character.</summary>
      ProseCraftBlocklist,

      /// <summary>
      ///   The PRISONERS bargain-offer teaching (<c>deliver_prisoner</c>). Dropped entirely in Lean; note that
      ///   in Full it is ALSO gated on <see cref="Models.EncounterContext.WarStatus" /> being resolved (task
      ///   6d), so this section only controls the Lean/Full split, not the war-status gate.
      /// </summary>
      DeliverPrisonerOffer
   }

   /// <summary>
   ///   Pure section-selection for the LEAN PROMPT mode: which heavy sections survive at a given level, and how
   ///   much conversation memory to keep. Small local models (short context) overflow the full prompt and return
   ///   nothing usable ("(My words escape me…)"); dropping the flavour-heavy sections keeps identity, persona,
   ///   format, current stance and the current encounter — enough for a coherent in-character reply. Tunable, and
   ///   unit-tested so the choices are pinned.
   /// </summary>
   public static class LeanPromptPolicy
   {
      /// <summary>Memory events kept in Lean mode (the most recent N); Full keeps them all.</summary>
      public const int LeanMemoryEventLimit = 6;

      /// <summary>
      ///   How many people in a crowded room may carry a recall line in the LEAN prompt. Two, deliberately: the
      ///   cost must not grow with the size of the party, or an eight-companion room would blow a small model's
      ///   context on its own.
      /// </summary>
      public const int LeanWitnessMemoryCount = 2;

      /// <summary>Characters of recall each of those two gets in Lean. Enough for one concrete fact, not a history.</summary>
      public const int LeanWitnessMemoryChars = 90;

      /// <summary>
      ///   The recall line a witness gets in the LEAN prompt: trimmed to <see cref="LeanWitnessMemoryChars" /> on a
      ///   word boundary, or null when there is nothing to say. Lean used to drop witness memory ENTIRELY, which
      ///   silently undid the 2026-08-23 fix (a companion forgetting an agreement struck one-on-one) for every
      ///   player on a small local model, the very players the mod's own setting tells to enable Lean. Dropping a
      ///   name, a persona and a gear line would have cost the room less than dropping what people REMEMBER: in a
      ///   group scene the memory is the load-bearing fact and the rest is decoration.
      /// </summary>
      public static string LeanWitnessMemory(string memory)
      {
         if (string.IsNullOrWhiteSpace(memory)) return null;

         string trimmed = memory.Trim();
         if (trimmed.Length <= LeanWitnessMemoryChars) return trimmed;

         int cut = trimmed.LastIndexOf(' ', LeanWitnessMemoryChars);

         return (cut > 0 ? trimmed.Substring(0, cut) : trimmed.Substring(0, LeanWitnessMemoryChars)).TrimEnd(',', '.', ';') + "...";
      }

      /// <summary>
      ///   True when a heavy section is included at the given level. Full keeps every section; Lean drops
      ///   each one explicitly, by name, so a new <see cref="PromptSection" /> member cannot silently ride
      ///   along as "included" without a deliberate decision here.
      /// </summary>
      public static bool Include(PromptSection section, LeanPromptLevel level)
      {
         if (level == LeanPromptLevel.Full) return true;

         return section switch {
            PromptSection.Relationships => false,
            PromptSection.WorldNarrative => false,
            PromptSection.CulturalBackground => false,
            PromptSection.AuthoredBackstory => false,
            PromptSection.FullBehaviorGuidelines => false,
            PromptSection.WitnessMachineryTeaching => false,
            PromptSection.ProseCraftBlocklist => false,
            PromptSection.DeliverPrisonerOffer => false,
            _ => false
         };
      }

      /// <summary>How many of the NPC's most recent memory events to inject: all in Full, a small window in Lean.</summary>
      public static int MemoryEventLimit(LeanPromptLevel level)
         => level == LeanPromptLevel.Full ? int.MaxValue : LeanMemoryEventLimit;

      /// <summary>True when the full file-authored behaviour guidelines are used; Lean substitutes a short built-in set.</summary>
      public static bool UseFullBehaviorGuidelines(LeanPromptLevel level)
         => Include(PromptSection.FullBehaviorGuidelines, level);
   }
}
