// Code written by Gabriel Mailhot, 31/07/2026.
// Age is never injected into the prompt today, so young lords (20-25) narrate themselves as if
// old and long-lived (player report). This turns a raw age into a natural life-stage phrase for
// the NPC identity prompt; kept pure so the bands are unit-tested independently of PromptBuilder.
// The number itself remains the anchor the LLM is told to respect; this phrase is only colour.

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>
   ///   Maps a hero's age in years to a short life-stage phrase for the identity directive.
   /// </summary>
   public static class LifeStageDescriber
   {
      /// <summary>
      ///   Describes the life stage matching <paramref name="age" />. Returns an empty string for
      ///   an unknown age (0 or negative), so the caller can omit the age line entirely for a
      ///   profile whose age was never captured (e.g. loaded from a save predating this field).
      /// </summary>
      public static string Describe(int age)
      {
         if (age <= 0) return "";
         if (age < 25) return "in the flush of youth";
         if (age < 40) return "in the prime of life";
         if (age < 55) return "in your middle years";
         return "an elder of many winters";
      }
   }
}
