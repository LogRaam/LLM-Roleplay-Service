// Code written by Gabriel Mailhot, 11/10/2026.
// The Prompt Composer (docs/design/prompt-composer/DESIGN.md in the mod repository, agreed by Gabriel 11/10/2026): CR's
// character prompt becomes a registry of KEYED sections walked in a layout, so power users and modders can reorder,
// turn off and reword them ("comme l'overclocking"). Until now the order lived only in the body of BuildSystemPrompt, and
// no section had a name a file could refer to. A key is a contract, like a save key: once shipped it is never renamed.

#region

using System;
using System.Collections.Generic;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Prompts
{
   /// <summary>Which side of the cache marker a section is written on.</summary>
   public enum PromptZone
   {
      /// <summary>Above the marker: sent as the cacheable prefix, must not change between two turns of a conversation.</summary>
      Prefix,

      /// <summary>The section that writes the marker itself (the encounter context): the boundary.</summary>
      Marker,

      /// <summary>Below the marker: sent fresh every turn; where anything true on one turn and gone the next belongs.</summary>
      Tail
   }

   /// <summary>How far a power user's layout may change a section (DESIGN.md, "Locks": only where the game breaks or safety is at stake).</summary>
   public enum PromptSectionLock
   {
      /// <summary>Free: may be moved, reworded or turned off.</summary>
      None,

      /// <summary>Cannot be turned off; may move within its zone; its wording may change.</summary>
      Required,

      /// <summary>Cannot be turned off, moved or reworded.</summary>
      Sealed
   }

   /// <summary>One section of the character prompt: its key, its zone, its lock, and how it is written.</summary>
   public sealed class PromptSectionSpec
   {
      internal PromptSectionSpec(string key, PromptZone zone, PromptSectionLock @lock, Action<PromptBuilder, StringBuilder, PromptBuildState> render)
      {
         Key = key;
         Zone = zone;
         Lock = @lock;
         Render = render;
      }

      /// <summary>The stable name a layout or a text file refers to (snake_case of the Append method it calls).</summary>
      public string Key { get; }

      /// <summary>Which side of the cache marker it is written on in the stock layout.</summary>
      public PromptZone Zone { get; }

      /// <summary>How far a layout may change it.</summary>
      public PromptSectionLock Lock { get; }

      /// <summary>Writes the section (or nothing, when its gate is shut) for one build.</summary>
      internal Action<PromptBuilder, StringBuilder, PromptBuildState> Render { get; }
   }

   /// <summary>What one build of the character prompt is about, handed to every section.</summary>
   internal sealed class PromptBuildState
   {
      /// <summary>The character speaking.</summary>
      public NpcProfile Npc { get; init; } = null!;

      /// <summary>The world as the host sees it.</summary>
      public WorldState World { get; init; } = null!;

      /// <summary>This encounter, or null for a bare build.</summary>
      public EncounterContext? Context { get; init; }

      /// <summary>Full or Lean (a small model's short prompt).</summary>
      public LeanPromptLevel Lean { get; init; }

      /// <summary>The {{variables}} of this build.</summary>
      public IReadOnlyDictionary<string, string> Vars { get; init; } = null!;
   }
}
